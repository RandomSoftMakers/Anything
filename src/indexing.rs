use std::cell::RefCell;
use std::path::PathBuf;
use std::rc::Rc;

use gtk4::glib;
use gtk4::prelude::*;

use libanything::Indexer;
use searchengine::SearchEngine;

use crate::{default_index_path, load_custom_skip_dirs, tr, tr_fmt, UiWidgets};

pub fn start_indexing(
    indexer: &Rc<RefCell<Option<Indexer>>>,
    engine: &Rc<RefCell<Option<SearchEngine>>>,
    ui: &UiWidgets,
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
