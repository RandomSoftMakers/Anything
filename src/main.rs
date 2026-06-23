use std::cell::RefCell;
use std::path::PathBuf;
use std::rc::Rc;
use std::sync::{OnceLock, RwLock};
#[cfg(not(windows))]
use std::sync::mpsc;

use gtk4::glib;
use gtk4::prelude::*;
use libadwaita::prelude::*;

use libanything::Indexer;
use searchengine::{SearchEngine, SearchType};

#[cfg(windows)]
fn load_windows_theme() {
    #[link(name = "dwmapi")]
    extern "system" {
        fn DwmGetColorizationColor(
            pcrColorization: *mut u32,
            pfOpaqueBlend: *mut i32,
        ) -> i32;
    }

    let accent_color = unsafe {
        let mut color: u32 = 0;
        let mut opaque: i32 = 0;
        if DwmGetColorizationColor(&mut color, &mut opaque) == 0 {
            Some(color)
        } else {
            None
        }
    };

    let accent_css = if let Some(color) = accent_color {
        // DwmGetColorizationColor returns 0xAABBGGRR, convert to #RRGGBB
        let r = color & 0xFF;
        let g = (color >> 8) & 0xFF;
        let b = (color >> 16) & 0xFF;
        format!(
            "@define-color accent #{:02x}{:02x}{:02x};\n\
             @define-color accent_bg #{:02x}{:02x}{:02x};\n\
             @define-color accent_fg #ffffff;\n\
             @define-color accent_hover #{:02x}{:02x}{:02x};\n\
             @define-color accent_active #{:02x}{:02x}{:02x};\n",
            r, g, b,
            r, g, b,
            r.saturating_sub(20), g.saturating_sub(20), b.saturating_sub(20),
            r.saturating_sub(40), g.saturating_sub(40), b.saturating_sub(40)
        )
    } else {
        String::new()
    };

    // Load static theme file
    let theme_path = Path::new(env!("CARGO_MANIFEST_DIR")).join("styles").join("windows.css");
    if theme_path.exists() {
        let provider = gtk4::CssProvider::new();
        if let Ok(css_data) = std::fs::read_to_string(&theme_path) {
            let full_css = accent_css + &css_data;
            let bytes = gtk4::glib::Bytes::from_owned(full_css.into_bytes());
            provider.load_from_bytes(&bytes);
            gtk4::style_context_add_provider_for_display(
                &gtk4::gdk::Display::default().unwrap(),
                &provider,
                gtk4::STYLE_PROVIDER_PRIORITY_APPLICATION,
            );
        }
    }
}

mod indexing;
mod lang;
mod settings;
#[cfg(not(windows))]
mod tray;

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

#[derive(Clone)]
struct FileResult {
    name: String,
    full_path: String,
}

pub fn home_dir() -> PathBuf {
    #[cfg(windows)]
    {
        std::env::var("USERPROFILE")
            .or_else(|_| std::env::var("HOME"))
            .map(PathBuf::from)
            .unwrap_or_else(|_| PathBuf::from("."))
    }
    #[cfg(not(windows))]
    {
        std::env::var("HOME")
            .map(PathBuf::from)
            .unwrap_or_else(|_| PathBuf::from("."))
    }
}

fn default_index_path() -> PathBuf {
    home_dir().join(".config/anything-index.anythingindex")
}

fn custom_skip_dirs_path() -> PathBuf {
    home_dir().join(".config/anything/custom_skip_dirs.txt")
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

#[derive(Clone)]
struct UiWidgets {
    window: libadwaita::ApplicationWindow,
    string_list: gtk4::StringList,
    status_label: gtk4::Label,
    spinner: gtk4::Box,
}

struct AppState {
    results: Rc<RefCell<Vec<FileResult>>>,
    engine: Rc<RefCell<Option<SearchEngine>>>,
    indexer: Rc<RefCell<Option<Indexer>>>,
}

impl AppState {
    fn new() -> Self {
        AppState {
            results: Rc::new(RefCell::new(vec![])),
            engine: Rc::new(RefCell::new(None)),
            indexer: Rc::new(RefCell::new(None)),
        }
    }
}

fn build_ui(app: &libadwaita::Application) -> libadwaita::ApplicationWindow {
    #[cfg(windows)]
    load_windows_theme();
    gtk4::Window::set_default_icon_name("io.github.anything");
    let _ = LANG.set(RwLock::new(lang::Lang::new()));

    let state = AppState::new();

    let window = libadwaita::ApplicationWindow::builder()
        .application(app)
        .title(&tr("window_title"))
        .default_width(960)
        .default_height(640)
        .build();

    let toolbar_view = libadwaita::ToolbarView::new();
    window.set_content(Some(&toolbar_view));

    let header_bar = libadwaita::HeaderBar::new();
    let title_label = gtk4::Label::new(Some("Anything"));
    title_label.add_css_class("title");
    header_bar.set_title_widget(Some(&title_label));

    let settings_btn = gtk4::Button::new();
    settings_btn.set_icon_name("open-menu-symbolic");

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
        let results = state.results.clone();
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

    let background_toggle = gtk4::Switch::new();
    background_toggle.set_active(true);
    background_toggle.set_valign(gtk4::Align::Center);

    let settings_win: Rc<RefCell<Option<libadwaita::PreferencesDialog>>> = Rc::new(RefCell::new(None));

    let index_path = default_index_path();
    if index_path.exists() {
        match SearchEngine::load(&index_path) {
            Ok(se) => {
                *state.engine.borrow_mut() = Some(se);
                let size = state.engine.borrow().as_ref().map(|e| e.index_size()).unwrap_or(0);
                status_label.set_text(&tr_fmt("status_ready", &[("count", &size.to_string())]));
            }
            Err(e) => {
                status_label.set_text(&tr_fmt("status_load_error", &[("error", &e.to_string())]));
                if background_toggle.is_active() {
                    indexing::start_indexing(&state.indexer, &state.engine, &ui);
                }
            }
        }
    } else if background_toggle.is_active() {
        indexing::start_indexing(&state.indexer, &state.engine, &ui);
    }

    search_entry.connect_search_changed({
        let engine = state.engine.clone();
        let results = state.results.clone();
        let ui = ui.clone();
        move |entry| {
            let query = entry.text().to_string();

            let guard = engine.borrow();
            let engine_ready = guard.is_some();
            drop(guard);

            if !engine_ready {
                ui.string_list.splice(0, ui.string_list.n_items(), &[] as &[&str]);
                if query.trim().is_empty() {
                    ui.status_label.set_text(&tr("status_indexing"));
                } else {
                    ui.status_label.set_text(&tr("status_building"));
                }
                return;
            }

            if query.trim().is_empty() {
                results.borrow_mut().clear();
                ui.string_list.splice(0, ui.string_list.n_items(), &[] as &[&str]);
                let size = engine.borrow().as_ref().map(|e| e.index_size()).unwrap_or(0);
                ui.status_label.set_text(&tr_fmt("status_ready", &[("count", &size.to_string())]));
                return;
            }

            let guard = engine.borrow();
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
            *results.borrow_mut() = file_results;

            let names: Vec<String> = results.borrow().iter().map(|r| r.name.clone()).collect();
            let name_refs: Vec<&str> = names.iter().map(|s| s.as_str()).collect();
            ui.string_list.splice(0, ui.string_list.n_items(), &name_refs);

            if count == 0 {
                ui.status_label.set_text(&tr("status_no_results"));
            } else {
                ui.status_label.set_text(&tr_fmt("status_results", &[("count", &count.to_string())]));
            }
        }
    });

    list_view.connect_activate({
        let results = state.results.clone();
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

    let state_s = AppState { results: state.results.clone(), engine: state.engine.clone(), indexer: state.indexer.clone() };
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
        let new_win = settings::build_settings_window(
            state_s.indexer.clone(),
            state_s.engine.clone(),
            ui_s.clone(),
            refreshable_s.clone(),
            bg_toggle_s.clone(),
            swin.clone(),
        );
        *swin.borrow_mut() = Some(new_win);
    });

    #[cfg(not(windows))]
    {
        let (tray_tx, tray_rx) = mpsc::channel();
        tray::setup_tray_polling(tray_rx, window.clone());
        tray::start_tray_thread(tray_tx);
    }

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
