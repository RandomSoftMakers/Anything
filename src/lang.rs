use std::collections::HashMap;
use std::path::PathBuf;

pub struct Lang {
    code: String,
    strings: HashMap<String, String>,
}

impl Lang {
    pub fn new() -> Self {
        let code = detect_lang().unwrap_or_else(|| "ru".to_string());
        Self::with_code(&code)
    }

    pub fn with_code(code: &str) -> Self {
        let mut strings = default_ru();

        if code != "ru" {
            if let Ok(loaded) = load_yaml(code) {
                for (k, v) in loaded {
                    strings.insert(k, v);
                }
            }
        }

        Lang {
            code: code.to_string(),
            strings,
        }
    }

    pub fn set_code(&mut self, code: &str) {
        let mut strings = default_ru();
        if code != "ru" {
            if let Ok(loaded) = load_yaml(code) {
                for (k, v) in loaded {
                    strings.insert(k, v);
                }
            }
        }
        self.code = code.to_string();
        self.strings = strings;
    }

    pub fn tr(&self, key: &str) -> String {
        self.strings
            .get(key)
            .cloned()
            .unwrap_or_else(|| key.to_string())
    }

    pub fn tr_fmt(&self, key: &str, args: &[(&str, &str)]) -> String {
        let mut s = self.tr(key);
        for (k, v) in args {
            s = s.replace(&format!("{{{}}}", k), v);
        }
        s
    }

    pub fn code(&self) -> &str {
        &self.code
    }
}

pub fn available_codes() -> Vec<(String, String)> {
    let mut codes: Vec<(String, String)> = Vec::new();
    codes.push(("ru".to_string(), "Русский".to_string()));

    // Scan config dir and flatpak dir for LANG.*.yaml files
    let dirs = vec![
        config_dir().ok(),
        Some(PathBuf::from("/app/share/anything")),
    ];
    for dir in dirs.into_iter().flatten() {
        if let Ok(entries) = std::fs::read_dir(&dir) {
            for entry in entries.flatten() {
                let name = entry.file_name();
                let name = name.to_string_lossy();
                if let Some(rest) = name.strip_prefix("LANG.") {
                    if let Some(code) = rest.strip_suffix(".yaml") {
                        if !codes.iter().any(|(c, _)| c == code) {
                            let label = match code {
                                "en" => "English".to_string(),
                                "ru" => continue,
                                other => other.to_string(),
                            };
                            codes.push((code.to_string(), label));
                        }
                    }
                }
            }
        }
    }

    codes
}

fn detect_lang() -> Option<String> {
    let lang = std::env::var("LANG").ok()?;
    let code = lang.split('.').next()?.split('_').next()?;
    Some(code.to_lowercase())
}

fn load_yaml(code: &str) -> Result<HashMap<String, String>, Box<dyn std::error::Error>> {
    let paths = vec![
        config_dir()?.join(format!("LANG.{}.yaml", code)),
        PathBuf::from(format!("/app/share/anything/LANG.{}.yaml", code)),
    ];
    for path in &paths {
        if path.exists() {
            let content = std::fs::read_to_string(path)?;
            let map: HashMap<String, String> = serde_yaml::from_str(&content)?;
            return Ok(map);
        }
    }
    Err("not found".into())
}

fn config_dir() -> Result<PathBuf, Box<dyn std::error::Error>> {
    let home = std::env::var("HOME")?;
    let dir = PathBuf::from(home).join(".config/anything");
    if !dir.exists() {
        std::fs::create_dir_all(&dir)?;
    }
    Ok(dir)
}

fn default_ru() -> HashMap<String, String> {
    let mut m = HashMap::new();
    m.insert("window_title".into(), "Anything — быстрый поиск файлов".into());
    m.insert("settings".into(), "Настройки".into());
    m.insert("bg_indexing".into(), "Индексация в фоне".into());
    m.insert("rebuild_index".into(), "Перестроить индекс".into());
    m.insert("custom_skip_dirs".into(), "Исключить каталоги".into());
    m.insert("add_dir".into(), "Добавить".into());
    m.insert("remove".into(), "Удалить".into());
    m.insert("light_theme".into(), "Светлая".into());
    m.insert("dark_theme".into(), "Тёмная".into());
    m.insert("about".into(), "О программе".into());
    m.insert("about_comments".into(), "Быстрый поиск файлов\n\nДвижок: Rust (LibAnything + SearchEngine)\nGUI: GTK4 + LibAdwaita".into());
    m.insert("search_placeholder".into(), "Введите запрос (например: !tmp ext:pdf \"отчёт\")".into());
    m.insert("status_init".into(), "Инициализация...".into());
    m.insert("status_ready".into(), "Готов к поиску (индекс: {count} записей)".into());
    m.insert("status_indexing".into(), "Индексация...".into());
    m.insert("status_indexing_progress".into(), "Индексация... {count} файлов".into());
    m.insert("status_building".into(), "Индекс ещё строится...".into());
    m.insert("status_loading_index".into(), "Загрузка индекса...".into());
    m.insert("status_load_error".into(), "Ошибка загрузки индекса: {error}".into());
    m.insert("status_error".into(), "Ошибка: {error}".into());
    m.insert("status_failed".into(), "Ошибка индексации".into());
    m.insert("status_timeout".into(), "Индексация прервана (таймаут 45 с)".into());
    m.insert("status_no_results".into(), "Совпадений не найдено".into());
    m.insert("status_results".into(), "Найдено: {count} совпадений".into());
    m.insert("settings_title".into(), "Настройки Anything".into());
    m.insert("language".into(), "Язык".into());
    m.insert("general".into(), "Основные".into());
    m.insert("excluded_dirs".into(), "Исключённые каталоги".into());
    m.insert("dir_placeholder".into(), "/путь/к/каталогу".into());
    m
}
