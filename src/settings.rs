use std::cell::RefCell;
use std::rc::Rc;

use gtk4::prelude::*;
use libadwaita::prelude::*;

use libanything::Indexer;
use searchengine::SearchEngine;

use crate::indexing::start_indexing;
use crate::{lang, load_custom_skip_dirs, save_custom_skip_dirs, tr, set_lang, LANG, RefreshableLabels, UiWidgets};

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

#[allow(clippy::too_many_arguments)]
pub fn build_settings_window(
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

    let actions_group = libadwaita::PreferencesGroup::new();
    actions_group.set_title(&tr("actions"));

    let rebuild_row = libadwaita::ActionRow::new();
    rebuild_row.set_title(&tr("rebuild_index"));
    rebuild_row.set_activatable(true);
    actions_group.add(&rebuild_row);

    page.add(&actions_group);

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

    rebuild_dir_list(&dirs.borrow(), &dir_list_box);
    connect_remove_buttons(&dir_list_box, dirs.clone());
    dirs_group.add(&dir_list_box);

    page.add(&dirs_group);

    win.add(&page);

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
    rebuild_row.connect_activated(move |_| {
        start_indexing(&indexer_rc, &engine_rc, &ui_rebuild);
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
