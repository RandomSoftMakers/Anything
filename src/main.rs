use std::cell::RefCell;
use std::path::PathBuf;
use std::rc::Rc;
use std::sync::mpsc;
use std::sync::{OnceLock, RwLock};

use gtk4::glib;
use gtk4::prelude::*;
use libadwaita::prelude::*;

use libanything::Indexer;
use searchengine::{SearchEngine, SearchType};
use zbus::blocking::Connection;
use zbus::interface;

mod lang;
static LANG: OnceLock<RwLock<lang::Lang>> = OnceLock::new();

fn tr(key: &str) -> String {
    LANG.get()
        .and_then(|l| l.read().ok())
        .map(|g| g.tr(key))
        .unwrap_or_else(|| key.to_string())
}

fn tr_fmt(key: &str, args: &[(&str, &str)]) -> String {
    LANG.get()
        .and_then(|l| l.read().ok())
        .map(|g| g.tr_fmt(key, args))
        .unwrap_or_else(|| key.to_string())
}

fn set_lang(code: &str) {
    if let Some(lang) = LANG.get() {
        if let Ok(mut w) = lang.write() {
            w.set_code(code);
        }
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Data
// ──────────────────────────────────────────────────────────────────────────────

#[derive(Clone)]
struct FileResult {
    name: String,
    full_path: String,
}

fn default_index_path() -> PathBuf {
    let home = std::env::var("HOME").unwrap_or_else(|_| ".".to_string());
    PathBuf::from(home).join(".config/anything-index.anythingindex")
}

fn custom_skip_dirs_path() -> PathBuf {
    let home = std::env::var("HOME").unwrap_or_else(|_| ".".to_string());
    PathBuf::from(home).join(".config/anything/custom_skip_dirs.txt")
}

fn load_custom_skip_dirs() -> Vec<String> {
    let path = custom_skip_dirs_path();
    match std::fs::read_to_string(&path) {
        Ok(content) => content
            .lines()
            .map(|l| l.trim().to_string())
            .filter(|l| !l.is_empty() && !l.starts_with('#'))
            .collect(),
        Err(_) => vec![],
    }
}

fn save_custom_skip_dirs(dirs: &[String]) {
    let mut content = String::new();
    for d in dirs {
        content.push_str(d);
        content.push('\n');
    }
    let _ = std::fs::write(custom_skip_dirs_path(), &content);
}

// ──────────────────────────────────────────────────────────────────────────────
// Refreshable main‑window labels
// ──────────────────────────────────────────────────────────────────────────────

#[derive(Clone)]
struct RefreshableLabels {
    window: libadwaita::ApplicationWindow,
    search_entry: gtk4::SearchEntry,
    theme_btn: gtk4::Button,
    is_dark: Rc<RefCell<bool>>,
}

impl RefreshableLabels {
    fn refresh(&self) {
        self.window.set_title(Some(&tr("window_title")));
        self.search_entry
            .set_placeholder_text(Some(&tr("search_placeholder")));
        if *self.is_dark.borrow() {
            self.theme_btn.set_tooltip_text(Some(&tr("light_theme")));
        } else {
            self.theme_btn.set_tooltip_text(Some(&tr("dark_theme")));
        }
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Tray
// ──────────────────────────────────────────────────────────────────────────────

#[derive(Debug)]
enum TrayEvent {
    Activate,
    Quit,
}

struct TrayIface {
    tx: mpsc::Sender<TrayEvent>,
}

#[interface(name = "org.kde.StatusNotifierItem")]
impl TrayIface {
    #[zbus(property)]
    fn category(&self) -> &str { "ApplicationStatus" }
    #[zbus(property)]
    fn id(&self) -> &str { "anything" }
    #[zbus(property)]
    fn title(&self) -> &str { "Anything" }
    #[zbus(property)]
    fn status(&self) -> &str { "Active" }
    #[zbus(property)]
    fn icon_name(&self) -> &str { "io.github.anything" }
    #[zbus(property)]
    fn icon_theme_path(&self) -> &str { "/app/share/icons" }
    #[zbus(property)]
    fn item_is_menu(&self) -> bool { false }
    #[zbus(property)]
    fn window_id(&self) -> i32 { 0 }

    fn activate(&self, _x: i32, _y: i32) {
        let _ = self.tx.send(TrayEvent::Activate);
    }
    fn secondary_activate(&self, _x: i32, _y: i32) {
        let _ = self.tx.send(TrayEvent::Quit);
    }
    fn context_menu(&self, _x: i32, _y: i32) {
        let _ = self.tx.send(TrayEvent::Quit);
    }
}

fn setup_tray(tx: mpsc::Sender<TrayEvent>) -> Result<(), Box<dyn std::error::Error>> {
    let conn = Connection::session()?;
    let iface = TrayIface { tx };
    conn.object_server().at("/StatusNotifierItem", iface)?;

    let ret = conn.call_method(
        Some("org.kde.StatusNotifierWatcher"),
        "/StatusNotifierWatcher",
        Some("org.kde.StatusNotifierWatcher"),
        "RegisterStatusNotifierItem",
        &"/StatusNotifierItem",
    );
    match ret {
        Ok(_) => eprintln!("tray: registered successfully at /StatusNotifierItem"),
        Err(e) => eprintln!("tray: RegisterStatusNotifierItem failed: {e}"),
    }

    loop { std::thread::sleep(std::time::Duration::from_secs(3600)); }
}

// ──────────────────────────────────────────────────────────────────────────────
// Build Settings Window
// ──────────────────────────────────────────────────────────────────────────────

#[allow(clippy::too_many_arguments)]
fn build_settings_window(
    indexer: Rc<RefCell<Option<Indexer>>>,
    engine: Rc<RefCell<Option<SearchEngine>>>,
    ui: UiWidgets,
    refreshable: RefreshableLabels,
    background_toggle: gtk4::Switch,
    settings_win_ref: Rc<RefCell<Option<libadwaita::PreferencesDialog>>>,
) -> libadwaita::PreferencesDialog {
    let win = libadwaita::PreferencesDialog::new();
    win.set_title(&tr("settings_title"));
    win.set_content_width(500);
    win.set_search_enabled(false);

    let page = libadwaita::PreferencesPage::new();

    // ── General ───────────────────────────────────────────────────────

    let general_group = libadwaita::PreferencesGroup::new();
    general_group.set_title(&tr("general"));

    let bg_row = libadwaita::ActionRow::new();
    bg_row.set_title(&tr("bg_indexing"));
    bg_row.add_suffix(&background_toggle);
    bg_row.set_activatable_widget(Some(&background_toggle));
    general_group.add(&bg_row);

    let codes = lang::available_codes();
    let lang_strings: Vec<String> = codes.iter().map(|(_, label)| label.clone()).collect();
    let lang_refs: Vec<&str> = lang_strings.iter().map(|s| s.as_str()).collect();
    let lang_model = gtk4::StringList::new(&lang_refs);

    let current_code = LANG
        .get()
        .and_then(|l| l.read().ok())
        .map(|g| g.code().to_string())
        .unwrap_or_else(|| "ru".to_string());

    let current_idx = codes
        .iter()
        .position(|(c, _)| c == &current_code)
        .unwrap_or(0);

    let lang_dropdown = gtk4::DropDown::new(Some(lang_model.clone()), None::<&gtk4::Expression>);
    lang_dropdown.set_selected(current_idx as u32);
    lang_dropdown.set_valign(gtk4::Align::Center);

    let lang_row = libadwaita::ActionRow::new();
    lang_row.set_title(&tr("language"));
    lang_row.add_suffix(&lang_dropdown);
    lang_row.set_activatable_widget(Some(&lang_dropdown));
    general_group.add(&lang_row);

    page.add(&general_group);

    // ── Actions ───────────────────────────────────────────────────────

    let actions_group = libadwaita::PreferencesGroup::new();
    actions_group.set_title(&tr("actions"));

    let rebuild_row = libadwaita::ActionRow::new();
    rebuild_row.set_title(&tr("rebuild_index"));
    rebuild_row.set_activatable(true);
    actions_group.add(&rebuild_row);

    page.add(&actions_group);

    // ── Excluded directories ──────────────────────────────────────────

    let dirs_group = libadwaita::PreferencesGroup::new();
    dirs_group.set_title(&tr("excluded_dirs"));

    let add_box = gtk4::Box::new(gtk4::Orientation::Horizontal, 6);
    add_box.set_margin_start(4);
    add_box.set_margin_end(4);
    add_box.set_margin_top(4);
    add_box.set_margin_bottom(4);
    let dir_entry = gtk4::Entry::new();
    dir_entry.set_placeholder_text(Some(&tr("dir_placeholder")));
    dir_entry.set_hexpand(true);
    let add_btn = gtk4::Button::from_icon_name("list-add-symbolic");
    add_btn.set_tooltip_text(Some(&tr("add_dir")));
    add_box.append(&dir_entry);
    add_box.append(&add_btn);
    dirs_group.add(&add_box);

    let dirs = Rc::new(RefCell::new(load_custom_skip_dirs()));
    let dir_list_box = gtk4::ListBox::new();
    dir_list_box.set_selection_mode(gtk4::SelectionMode::None);
    dir_list_box.set_vexpand(true);

    fn rebuild_dir_list(dirs: &[String], list_box: &gtk4::ListBox) {
        while let Some(child) = list_box.first_child() {
            list_box.remove(&child);
        }
        for d in dirs {
            let row = gtk4::Box::new(gtk4::Orientation::Horizontal, 6);
            row.set_margin_top(4);
            row.set_margin_bottom(4);
            row.set_margin_start(8);
            row.set_margin_end(4);
            let label = gtk4::Label::new(Some(d));
            label.set_halign(gtk4::Align::Start);
            label.set_hexpand(true);
            label.set_ellipsize(gtk4::pango::EllipsizeMode::End);
            let rm_btn = gtk4::Button::from_icon_name("user-trash-symbolic");
            rm_btn.set_css_classes(&["destructive-action", "flat"]);
            rm_btn.set_tooltip_text(Some(&tr("remove")));
            row.append(&label);
            row.append(&rm_btn);
            let list_row = gtk4::ListBoxRow::new();
            list_row.set_child(Some(&row));
            list_box.append(&list_row);
        }
    }

    rebuild_dir_list(&dirs.borrow(), &dir_list_box);

    fn connect_remove_buttons(
        list_box: &gtk4::ListBox,
        dirs: Rc<RefCell<Vec<String>>>,
    ) {
        let mut idx = 0usize;
        let mut child = list_box.first_child();
        while let Some(row) = child {
            if let Some(list_row) = row.downcast_ref::<gtk4::ListBoxRow>() {
                if let Some(row_box) = list_row.child().and_downcast::<gtk4::Box>() {
                    if let Some(btn) = row_box.last_child().and_downcast::<gtk4::Button>() {
                        let btn_dirs = dirs.clone();
                        let btn_list = list_box.clone();
                        btn.connect_clicked(move |_| {
                            let mut guard = btn_dirs.borrow_mut();
                            if idx < guard.len() {
                                guard.remove(idx);
                                save_custom_skip_dirs(&guard);
                                rebuild_dir_list(&guard, &btn_list);
                                connect_remove_buttons(&btn_list, btn_dirs.clone());
                            }
                        });
                    }
                }
            }
            idx += 1;
            child = row.next_sibling();
        }
    }

    connect_remove_buttons(&dir_list_box, dirs.clone());
    dirs_group.add(&dir_list_box);

    page.add(&dirs_group);

    win.add(&page);

    // ── Handlers ───────────────────────────────────────────────────────

    let codes_for_lang = codes.clone();
    let refreshable_lang = refreshable.clone();
    let settings_win_lang = settings_win_ref.clone();
    lang_dropdown.connect_selected_notify(move |dd| {
        let idx = dd.selected() as usize;
        if let Some((code, _)) = codes_for_lang.get(idx) {
            set_lang(code);
            refreshable_lang.refresh();
            if let Some(swin) = settings_win_lang.borrow().as_ref() {
                swin.set_title(&tr("settings_title"));
            }
        }
    });

    let indexer_rc = indexer.clone();
    let engine_rc = engine.clone();
    let ui_rebuild = ui.clone();
    let bg_toggle = background_toggle.clone();
    rebuild_row.connect_activated(move |_| {
        start_indexing(&indexer_rc, &engine_rc, &ui_rebuild, &bg_toggle);
    });

    let dirs_add = dirs.clone();
    let dlb = dir_list_box.clone();
    add_btn.connect_clicked(move |_| {
        let text = dir_entry.text().to_string().trim().to_string();
        if text.is_empty() {
            return;
        }
        dir_entry.set_text("");
        let mut guard = dirs_add.borrow_mut();
        guard.push(text);
        save_custom_skip_dirs(&guard);
        rebuild_dir_list(&guard, &dlb);
        connect_remove_buttons(&dlb, dirs_add.clone());
    });

    win.connect_closed({
        let swin = settings_win_ref.clone();
        move |_| {
            *swin.borrow_mut() = None;
        }
    });

    win.present(Some(&ui.window));
    win
}

// ──────────────────────────────────────────────────────────────────────────────
// UI helpers
// ──────────────────────────────────────────────────────────────────────────────

#[derive(Clone)]
struct UiWidgets {
    window: libadwaita::ApplicationWindow,
    string_list: gtk4::StringList,
    status_label: gtk4::Label,
    spinner: gtk4::Box,
}

fn build_ui(app: &libadwaita::Application) -> libadwaita::ApplicationWindow {
    gtk4::Window::set_default_icon_name("io.github.anything");
    let _ = LANG.set(RwLock::new(lang::Lang::new()));

    let results: Rc<RefCell<Vec<FileResult>>> = Rc::new(RefCell::new(vec![]));
    let engine: Rc<RefCell<Option<SearchEngine>>> = Rc::new(RefCell::new(None));
    let indexer: Rc<RefCell<Option<Indexer>>> = Rc::new(RefCell::new(None));

    // ── Window ────────────────────────────────────────────────────────────

    let window = libadwaita::ApplicationWindow::builder()
        .application(app)
        .title(&tr("window_title"))
        .default_width(960)
        .default_height(640)
        .build();

    let toolbar_view = libadwaita::ToolbarView::new();
    window.set_content(Some(&toolbar_view));

    // ── Header bar ────────────────────────────────────────────────────────

    let header_bar = libadwaita::HeaderBar::new();

    let title_label = gtk4::Label::new(Some("Anything"));
    title_label.add_css_class("title");
    header_bar.set_title_widget(Some(&title_label));

    // Settings button opens a separate window
    let settings_btn = gtk4::Button::new();
    settings_btn.set_icon_name("open-menu-symbolic");

    // Theme toggle
    let style_manager = libadwaita::StyleManager::default();
    let is_dark = Rc::new(RefCell::new(
        style_manager.color_scheme() == libadwaita::ColorScheme::ForceDark
            || style_manager.is_dark(),
    ));

    let theme_btn = gtk4::Button::new();
    if *is_dark.borrow() {
        theme_btn.set_icon_name("weather-clear-symbolic");
        theme_btn.set_tooltip_text(Some(&tr("light_theme")));
    } else {
        theme_btn.set_icon_name("weather-clear-night-symbolic");
        theme_btn.set_tooltip_text(Some(&tr("dark_theme")));
    }

    theme_btn.connect_clicked({
        let is_dark = is_dark.clone();
        let style_manager = style_manager.clone();
        move |btn| {
            let dark = *is_dark.borrow();
            if dark {
                style_manager.set_color_scheme(libadwaita::ColorScheme::ForceLight);
                btn.set_icon_name("weather-clear-night-symbolic");
                btn.set_tooltip_text(Some(&tr("dark_theme")));
            } else {
                style_manager.set_color_scheme(libadwaita::ColorScheme::ForceDark);
                btn.set_icon_name("weather-clear-symbolic");
                btn.set_tooltip_text(Some(&tr("light_theme")));
            }
            *is_dark.borrow_mut() = !dark;
        }
    });

    let about_btn = gtk4::Button::new();
    about_btn.set_icon_name("help-about-symbolic");
    about_btn.set_tooltip_text(Some(&tr("about")));

    header_bar.pack_start(&settings_btn);
    header_bar.pack_end(&theme_btn);
    header_bar.pack_end(&about_btn);
    toolbar_view.add_top_bar(&header_bar);

    // ── Content ───────────────────────────────────────────────────────────

    let content = gtk4::Box::new(gtk4::Orientation::Vertical, 8);
    content.set_margin_start(16);
    content.set_margin_end(16);
    content.set_margin_top(8);
    content.set_margin_bottom(16);
    toolbar_view.set_content(Some(&content));

    let search_entry = gtk4::SearchEntry::new();
    search_entry.set_placeholder_text(Some(&tr("search_placeholder")));
    search_entry.set_search_delay(300);
    content.append(&search_entry);

    let spinner = gtk4::Box::new(gtk4::Orientation::Vertical, 0);
    spinner.set_halign(gtk4::Align::Center);
    spinner.set_valign(gtk4::Align::Center);
    spinner.set_visible(false);

    let overlay = gtk4::Overlay::new();
    overlay.set_width_request(64);
    overlay.set_height_request(64);

    let folder_image = gtk4::Image::from_icon_name("folder-symbolic");
    folder_image.set_pixel_size(64);
    overlay.set_child(Some(&folder_image));

    let scan_area = gtk4::DrawingArea::new();
    scan_area.set_halign(gtk4::Align::Fill);
    scan_area.set_valign(gtk4::Align::Fill);

    let angle = Rc::new(RefCell::new(0.0_f64));
    {
        let angle = angle.clone();
        scan_area.set_draw_func(move |_, cr, w, h| {
            let a = *angle.borrow();
            let cx = w as f64 / 2.0;
            let cy = h as f64 / 2.0;
            let hw = w as f64 * 0.38;
            let hh = h as f64 * 0.35;

            let mx = cx + hw * (3.0 * a).sin();
            let my = cy + hh * (2.0 * a).sin();

            let _ = cr.save();
            cr.arc(mx, my, 6.0, 0.0, 2.0 * std::f64::consts::PI);
            cr.set_source_rgba(0.3, 0.7, 1.0, 0.85);
            cr.set_line_width(1.8);
            let _ = cr.stroke();
            let dx = (3.0 * a).cos();
            let dy = (2.0 * a).cos();
            let len = (dx * dx + dy * dy).sqrt();
            if len > 0.0 {
                let nx = dx / len;
                let ny = dy / len;
                cr.move_to(mx + 3.5 * nx, my + 3.5 * ny);
                cr.line_to(mx + 12.0 * nx, my + 12.0 * ny);
                cr.set_source_rgba(0.3, 0.7, 1.0, 0.85);
                cr.set_line_width(2.2);
                let _ = cr.stroke();
            }
            let _ = cr.restore();
        });
    }

    overlay.add_overlay(&scan_area);
    spinner.append(&overlay);

    {
        let angle = angle.clone();
        let scan_area = scan_area.clone();
        glib::timeout_add_local(std::time::Duration::from_millis(33), move || {
            *angle.borrow_mut() += 0.07;
            scan_area.queue_draw();
            glib::ControlFlow::Continue
        });
    }

    content.append(&spinner);

    let scrolled = gtk4::ScrolledWindow::new();
    scrolled.set_vexpand(true);

    let string_list = gtk4::StringList::new(&[] as &[&str]);
    let selection = gtk4::SingleSelection::new(Some(string_list.clone()));

    let factory = gtk4::SignalListItemFactory::new();

    factory.connect_setup(|_, obj| {
        let list_item = obj.downcast_ref::<gtk4::ListItem>().unwrap();
        let label = gtk4::Label::new(None);
        label.set_halign(gtk4::Align::Start);
        label.set_valign(gtk4::Align::Center);
        label.set_ellipsize(gtk4::pango::EllipsizeMode::End);
        label.set_max_width_chars(80);
        list_item.set_child(Some(&label));
    });

    factory.connect_bind({
        let results = results.clone();
        move |_, obj| {
            let list_item = obj.downcast_ref::<gtk4::ListItem>().unwrap();
            let pos = list_item.position();
            let label = list_item
                .child()
                .and_downcast::<gtk4::Label>()
                .expect("expected label");
            let guard = results.borrow();
            if let Some(r) = guard.get(pos as usize) {
                let name_escaped = glib::markup_escape_text(&r.name);
                let path_escaped = glib::markup_escape_text(&r.full_path);
                label.set_markup(&format!(
                    "<b>{}</b>\n<span size='small' alpha='50%'>{}</span>",
                    name_escaped, path_escaped
                ));
            } else {
                label.set_markup("");
            }
        }
    });

    let list_view = gtk4::ListView::new(Some(selection), Some(factory));
    list_view.set_single_click_activate(true);
    scrolled.set_child(Some(&list_view));
    content.append(&scrolled);

    let status_label = gtk4::Label::new(Some(&tr("status_init")));
    status_label.set_halign(gtk4::Align::Start);
    status_label.add_css_class("caption");
    content.append(&status_label);

    let ui = UiWidgets {
        window: window.clone(),
        string_list: string_list.clone(),
        status_label: status_label.clone(),
        spinner: spinner.clone(),
    };

    let refreshable = RefreshableLabels {
        window: window.clone(),
        search_entry: search_entry.clone(),
        theme_btn: theme_btn.clone(),
        is_dark: is_dark.clone(),
    };

    // ── Background toggle ──────────────────────────────────────────────

    let background_toggle = gtk4::Switch::new();
    background_toggle.set_active(true);
    background_toggle.set_valign(gtk4::Align::Center);

    // ── Settings window reference ──────────────────────────────────────

    let settings_win: Rc<RefCell<Option<libadwaita::PreferencesDialog>>> = Rc::new(RefCell::new(None));

    // ── Load or build index ───────────────────────────────────────────────

    let index_path = default_index_path();
    let engine_rc = engine.clone();
    let ui2 = ui.clone();

    if index_path.exists() {
        match SearchEngine::load(&index_path) {
            Ok(se) => {
                *engine_rc.borrow_mut() = Some(se);
                let size = engine_rc.borrow().as_ref().map(|e| e.index_size()).unwrap_or(0);
                status_label.set_text(&tr_fmt("status_ready", &[("count", &size.to_string())]));
            }
            Err(e) => {
                status_label.set_text(&tr_fmt("status_load_error", &[("error", &e.to_string())]));
                start_indexing(&indexer, &engine_rc, &ui2, &background_toggle);
            }
        }
    } else {
        start_indexing(&indexer, &engine_rc, &ui2, &background_toggle);
    }

    // ── Search callback ───────────────────────────────────────────────────

    let engine_rc2 = engine.clone();
    let results2 = results.clone();
    let ui3 = ui.clone();

    search_entry.connect_search_changed(move |entry| {
        let query = entry.text().to_string();

        let guard = engine_rc2.borrow();
        let engine_ready = guard.is_some();
        drop(guard);

        if !engine_ready {
            ui3.string_list.splice(0, ui3.string_list.n_items(), &[] as &[&str]);
            if query.trim().is_empty() {
                ui3.status_label.set_text(&tr("status_indexing"));
            } else {
                ui3.status_label.set_text(&tr("status_building"));
            }
            return;
        }

        if query.trim().is_empty() {
            results2.borrow_mut().clear();
            ui3.string_list.splice(0, ui3.string_list.n_items(), &[] as &[&str]);
            let size = engine_rc2.borrow().as_ref().map(|e| e.index_size()).unwrap_or(0);
            ui3.status_label.set_text(&tr_fmt("status_ready", &[("count", &size.to_string())]));
            return;
        }

        let guard = engine_rc2.borrow();
        let file_results: Vec<FileResult> = match guard.as_ref() {
            Some(engine) => engine.search(&query, SearchType::Fuzzy)
                .iter()
                .map(|r| FileResult {
                    name: std::path::Path::new(&r.name)
                        .file_name()
                        .map(|n| n.to_string_lossy().into_owned())
                        .unwrap_or_else(|| r.name.clone()),
                    full_path: r.name.clone(),
                })
                .collect(),
            None => Vec::new(),
        };
        let count = file_results.len();
        drop(guard);
        *results2.borrow_mut() = file_results;

        let names: Vec<String> = results2.borrow().iter().map(|r| r.name.clone()).collect();
        let name_refs: Vec<&str> = names.iter().map(|s| s.as_str()).collect();
        ui3.string_list.splice(0, ui3.string_list.n_items(), &name_refs);

        if count == 0 {
            ui3.status_label.set_text(&tr("status_no_results"));
        } else {
            ui3.status_label.set_text(&tr_fmt("status_results", &[("count", &count.to_string())]));
        }
    });

    // ── Open parent directory ─────────────────────────────────────────────

    list_view.connect_activate({
        let results = results.clone();
        let ui = ui.clone();
        move |_, position| {
            let full = results
                .borrow()
                .get(position as usize)
                .map(|r| r.full_path.clone());
            if let Some(ref path) = full {
                if let Some(parent) = std::path::Path::new(path).parent() {
                    let uri = format!("file://{}", parent.display());
                    let _ = gtk4::UriLauncher::new(&uri).launch(
                        Some(&ui.window),
                        None::<&gtk4::gio::Cancellable>,
                        |result| {
                            if let Err(e) = result {
                                log::warn!("Failed to open directory: {}", e);
                            }
                        },
                    );
                }
            }
        }
    });

    // ── About dialog ──────────────────────────────────────────────────────

    let about_window = window.clone();
    about_btn.connect_clicked(move |_| {
        let about = libadwaita::AboutDialog::new();
        about.set_application_name("Anything");
        about.set_application_icon("io.github.anything");
        about.set_version("2.0");
        about.set_developer_name("AnythingDevelopmentTeam");
        about.set_license_type(gtk4::License::Gpl30);
        about.set_comments(&tr("about_comments"));
        about.set_website("https://AnythingDevelopmentTeam.github.io");
        about.present(Some(&about_window));
    });

    // ── Settings button ─────────────────────────────────────────────────

    let indexer_s = indexer.clone();
    let engine_s = engine.clone();
    let ui_s = ui.clone();
    let refreshable_s = refreshable.clone();
    let bg_toggle_s = background_toggle.clone();
    let swin = settings_win.clone();

    settings_btn.connect_clicked(move |_| {
        if let Some(win) = swin.borrow().as_ref() {
            win.present(Some(&ui_s.window));
            win.grab_focus();
            return;
        }
        let new_win = build_settings_window(
            indexer_s.clone(),
            engine_s.clone(),
            ui_s.clone(),
            refreshable_s.clone(),
            bg_toggle_s.clone(),
            swin.clone(),
        );
        *swin.borrow_mut() = Some(new_win);
    });

    // ── Tray ────────────────────────────────────────────────────────────

    let (tray_tx, tray_rx) = mpsc::channel();

    let tray_window = window.clone();
    let tray_rx_app = app.clone();

    glib::timeout_add_local(std::time::Duration::from_millis(200), move || {
        while let Ok(event) = tray_rx.try_recv() {
            match event {
                TrayEvent::Activate => {
                    if tray_window.is_visible() {
                        tray_window.set_visible(false);
                    } else {
                        tray_window.present();
                    }
                }
                TrayEvent::Quit => {
                    tray_rx_app.quit();
                }
            }
        }
        glib::ControlFlow::Continue
    });

    std::thread::spawn(move || {
        if let Err(e) = setup_tray(tray_tx) {
            eprintln!("tray setup failed: {e}");
            log::warn!("tray: {}", e);
        }
    });

    // Ctrl+Q to quit
    let quit_win = window.clone();
    window.connect_close_request(move |_| {
        let _ = quit_win.set_visible(false);
        glib::Propagation::Stop
    });
    let app_clone = app.clone();
    let action_quit = gtk4::gio::SimpleAction::new("quit", None);
    action_quit.connect_activate(move |_, _| {
        app_clone.quit();
    });
    app.add_action(&action_quit);
    app.set_accels_for_action("app.quit", &["<Primary>q"]);

    window
}

// ──────────────────────────────────────────────────────────────────────────────
// Background indexing
// ──────────────────────────────────────────────────────────────────────────────

fn start_indexing(
    indexer: &Rc<RefCell<Option<Indexer>>>,
    engine: &Rc<RefCell<Option<SearchEngine>>>,
    ui: &UiWidgets,
    _bg_toggle: &gtk4::Switch,
) {
    let index_path = default_index_path();

    let mut idx = Indexer::new(index_path.clone());

    let ignore_path = PathBuf::from(
        std::env::var("HOME").unwrap_or_else(|_| ".".to_string()),
    )
    .join(".config/anything/IgnoreConfig.yaml");
    let mut ignore_cfg = match SearchEngine::load_ignore_config_yaml(&ignore_path) {
        Ok(cfg) => cfg,
        Err(_) => SearchEngine::default_ignore_config(),
    };

    for dir in load_custom_skip_dirs() {
        if !ignore_cfg.skip_dir_prefixes.contains(&dir) {
            ignore_cfg.skip_dir_prefixes.push(dir);
        }
    }
    idx.set_ignore_config(ignore_cfg);

    idx.start();
    *indexer.borrow_mut() = Some(idx);

    ui.status_label.set_text(&tr("status_indexing"));
    ui.spinner.set_visible(true);

    let engine_clone = engine.clone();
    let ui_clone = ui.clone();
    let indexer_clone = indexer.clone();
    let last_partial_sync = Rc::new(RefCell::new(std::time::Instant::now()));
    let lps = last_partial_sync.clone();

    let timer = glib::timeout_add_local(std::time::Duration::from_millis(500), move || {
        let guard = indexer_clone.borrow();
        let idx = match guard.as_ref() {
            Some(i) => i,
            None => return glib::ControlFlow::Break,
        };

        match idx.status() {
            libanything::IndexerStatus::Running => {
                let progress = idx.progress();
                ui_clone.status_label.set_text(&tr_fmt(
                    "status_indexing_progress",
                    &[("count", &progress.to_string())],
                ));

                if lps.borrow().elapsed().as_secs() >= 2 {
                    let partial = idx.partial_records();
                    if partial.len() > 1 {
                        let se = SearchEngine::from_records(partial);
                        *engine_clone.borrow_mut() = Some(se);
                        *lps.borrow_mut() = std::time::Instant::now();
                    }
                }

                glib::ControlFlow::Continue
            }
            libanything::IndexerStatus::Completed => {
                ui_clone.status_label.set_text(&tr("status_loading_index"));
                match SearchEngine::load(&index_path) {
                    Ok(se) => {
                        let size = se.index_size();
                        *engine_clone.borrow_mut() = Some(se);
                        ui_clone.spinner.set_visible(false);
                        ui_clone.status_label.set_text(&tr_fmt(
                            "status_ready",
                            &[("count", &size.to_string())],
                        ));
                    }
                    Err(e) => {
                        ui_clone.spinner.set_visible(false);
                        ui_clone.status_label.set_text(&tr_fmt("status_error", &[("error", &e.to_string())]));
                    }
                }
                glib::ControlFlow::Break
            }
            libanything::IndexerStatus::Failed => {
                ui_clone.spinner.set_visible(false);
                ui_clone.status_label.set_text(&tr("status_failed"));
                glib::ControlFlow::Break
            }
            libanything::IndexerStatus::Idle => {
                ui_clone.spinner.set_visible(false);
                ui_clone.status_label.set_text(&tr("status_timeout"));
                glib::ControlFlow::Break
            }
        }
    });

    let _ = timer;
}

// ──────────────────────────────────────────────────────────────────────────────
// Entry point
// ──────────────────────────────────────────────────────────────────────────────

fn main() {
    env_logger::init();
    let app = libadwaita::Application::builder()
        .application_id("io.github.anything")
        .build();

    let window = Rc::new(RefCell::new(None::<libadwaita::ApplicationWindow>));
    let w = window.clone();

    app.connect_activate(move |app| {
        let mut guard = w.borrow_mut();
        if let Some(win) = guard.as_ref() {
            win.present();
        } else {
            let win = build_ui(app);
            win.present();
            *guard = Some(win);
        }
    });

    app.run();
}
