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

fn icon_for_file(path: &str) -> &'static str {
    let p = std::path::Path::new(path);
    if p.is_dir() {
        return "folder-symbolic";
    }
    let name = p.file_name().and_then(|n| n.to_str()).unwrap_or("");
    if name.starts_with('.') && !name.eq_ignore_ascii_case(".ds_store") {
        return "text-x-generic";
    }
    let ext = p.extension().and_then(|e| e.to_str()).unwrap_or("").to_lowercase();
    match ext.as_str() {
        "png" | "jpg" | "jpeg" | "gif" | "bmp" | "svg" | "webp" | "ico" => "image-x-generic",
        "mp4" | "avi" | "mkv" | "mov" | "wmv" | "flv" | "webm" => "video-x-generic",
        "mp3" | "wav" | "flac" | "ogg" | "wma" | "aac" => "audio-x-generic",
        "zip" | "tar" | "gz" | "bz2" | "xz" | "7z" | "rar" | "zst" => "package-x-generic",
        "pdf" | "djvu" => "x-office-document",
        "doc" | "docx" | "odt" | "rtf" | "tex" => "x-office-document",
        "xls" | "xlsx" | "ods" | "csv" => "x-office-spreadsheet",
        "ppt" | "pptx" | "odp" => "x-office-presentation",
        "rs" | "py" | "js" | "ts" | "c" | "cpp" | "h" | "java" | "go" | "rb" | "sh" | "toml" | "json" | "yaml" | "yml" | "xml" | "html" | "css" => "text-x-code",
        "exe" | "msi" | "appimage" | "deb" | "rpm" | "bin" => "application-x-executable",
        "iso" | "img" => "media-optical",
        _ => "text-x-generic",
    }
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

fn theme_setting_path() -> PathBuf {
    home_dir().join(".config/anything/theme")
}

fn load_theme_setting() -> Option<bool> {
    std::fs::read_to_string(theme_setting_path())
        .ok()
        .and_then(|s| match s.trim() {
            "dark" => Some(true),
            "light" => Some(false),
            _ => None,
        })
}

fn save_theme_setting(dark: bool) {
    let content = if dark { "dark" } else { "light" };
    let _ = std::fs::write(theme_setting_path(), content);
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
    mode_btn: gtk4::ToggleButton,
    search_type: Rc<RefCell<SearchType>>,
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
        if *self.search_type.borrow() == SearchType::Exact {
            self.mode_btn.set_tooltip_text(Some(&tr("mode_exact")));
        } else {
            self.mode_btn.set_tooltip_text(Some(&tr("mode_fuzzy")));
        }
    }
}

#[derive(Clone)]
struct UiWidgets {
    window: libadwaita::ApplicationWindow,
    string_list: gtk4::StringList,
    status_label: gtk4::Label,
    spinner: gtk4::Spinner,
    search_type: Rc<RefCell<SearchType>>,
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
    let saved_dark = load_theme_setting();
    if let Some(dark) = saved_dark {
        style_manager.set_color_scheme(if dark {
            libadwaita::ColorScheme::ForceDark
        } else {
            libadwaita::ColorScheme::ForceLight
        });
    }
    let is_dark = Rc::new(RefCell::new(
        saved_dark.unwrap_or_else(|| {
            style_manager.color_scheme() == libadwaita::ColorScheme::ForceDark
                || style_manager.is_dark()
        })
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
            save_theme_setting(!dark);
        }
    });

    let search_type = Rc::new(RefCell::new(SearchType::Exact));

    let mode_btn = gtk4::ToggleButton::new();
    mode_btn.set_icon_name("edit-find-symbolic");
    mode_btn.set_tooltip_text(Some(&tr("mode_exact")));
    mode_btn.add_css_class("flat");

    {
        let search_type = search_type.clone();
        let mode_btn = mode_btn.clone();
        mode_btn.connect_toggled(move |btn| {
            let is_exact = *search_type.borrow() == SearchType::Exact;
            if is_exact {
                *search_type.borrow_mut() = SearchType::Fuzzy;
                btn.set_tooltip_text(Some(&tr("mode_fuzzy")));
            } else {
                *search_type.borrow_mut() = SearchType::Exact;
                btn.set_tooltip_text(Some(&tr("mode_exact")));
            }
        });
    }

    let about_btn = gtk4::Button::new();
    about_btn.set_icon_name("help-about-symbolic");
    about_btn.set_tooltip_text(Some(&tr("about")));

    header_bar.pack_start(&settings_btn);
    header_bar.pack_end(&theme_btn);
    header_bar.pack_end(&mode_btn);
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

    let spinner = gtk4::Spinner::new();
    spinner.set_halign(gtk4::Align::Center);
    spinner.set_valign(gtk4::Align::Center);
    spinner.set_size_request(32, 32);
    spinner.set_visible(false);
    content.append(&spinner);

    let scrolled = gtk4::ScrolledWindow::new();
    scrolled.set_vexpand(true);

    let string_list = gtk4::StringList::new(&[] as &[&str]);
    let selection = gtk4::SingleSelection::new(Some(string_list.clone()));

    let factory = gtk4::SignalListItemFactory::new();

    factory.connect_setup(|_, obj| {
        let list_item = obj.downcast_ref::<gtk4::ListItem>().unwrap();

        let row = gtk4::Box::new(gtk4::Orientation::Horizontal, 10);
        row.set_margin_top(6);
        row.set_margin_bottom(6);
        row.set_margin_start(8);
        row.set_margin_end(8);

        let icon = gtk4::Image::new();
        icon.set_pixel_size(28);
        icon.set_valign(gtk4::Align::Center);

        let text_box = gtk4::Box::new(gtk4::Orientation::Vertical, 1);
        let name_label = gtk4::Label::new(None);
        name_label.set_halign(gtk4::Align::Start);
        name_label.set_xalign(0.0);
        name_label.set_ellipsize(gtk4::pango::EllipsizeMode::End);
        name_label.add_css_class("body");

        let path_label = gtk4::Label::new(None);
        path_label.set_halign(gtk4::Align::Start);
        path_label.set_xalign(0.0);
        path_label.set_ellipsize(gtk4::pango::EllipsizeMode::End);
        path_label.add_css_class("caption");

        text_box.append(&name_label);
        text_box.append(&path_label);

        row.append(&icon);
        row.append(&text_box);

        list_item.set_child(Some(&row));
    });

    factory.connect_bind({
        let results = state.results.clone();
        move |_, obj| {
            let list_item = obj.downcast_ref::<gtk4::ListItem>().unwrap();
            let pos = list_item.position();
            let row = list_item
                .child()
                .and_downcast::<gtk4::Box>()
                .expect("expected box");
            let guard = results.borrow();
            if let Some(r) = guard.get(pos as usize) {
                if let Some(icon) = row.first_child().and_downcast::<gtk4::Image>() {
                    icon.set_icon_name(Some(icon_for_file(&r.full_path)));
                }
                if let Some(text_box) = row.last_child().and_downcast::<gtk4::Box>() {
                    if let Some(name_lbl) = text_box.first_child().and_downcast::<gtk4::Label>() {
                        name_lbl.set_text(&r.name);
                    }
                    if let Some(path_lbl) = text_box.last_child().and_downcast::<gtk4::Label>() {
                        path_lbl.set_text(&r.full_path);
                    }
                }
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
        search_type: search_type.clone(),
    };

    let refreshable = RefreshableLabels {
        window: window.clone(),
        search_entry: search_entry.clone(),
        theme_btn: theme_btn.clone(),
        mode_btn: mode_btn.clone(),
        search_type: search_type.clone(),
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

            let st = *ui.search_type.borrow();
            let guard = engine.borrow();
            let file_results: Vec<FileResult> = match guard.as_ref() {
                Some(engine) => engine.search(&query, st)
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
        move |_, position| {
            let full = results
                .borrow()
                .get(position as usize)
                .map(|r| r.full_path.clone());
            if let Some(ref path) = full {
                let uri = format!("file://{}", path);
                let _ = gtk4::UriLauncher::new(&uri).launch(
                    None::<&gtk4::Window>,
                    None::<&gtk4::gio::Cancellable>,
                    |result| {
                        if let Err(e) = result {
                            log::warn!("Failed to open file: {}", e);
                        }
                    },
                );
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

    window.set_size_request(400, 300);

    let entry = search_entry.clone();
    glib::idle_add_local(move || {
        entry.grab_focus();
        glib::ControlFlow::Break
    });

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
