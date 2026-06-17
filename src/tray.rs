#![cfg(not(windows))]
use std::sync::mpsc;

use gtk4::glib;
use gtk4::prelude::*;
use zbus::blocking::Connection;
use zbus::interface;

#[derive(Debug)]
pub enum TrayEvent {
    Activate,
}

pub struct TrayIface {
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
        let _ = self.tx.send(TrayEvent::Activate);
    }
    fn context_menu(&self, _x: i32, _y: i32) {
        let _ = self.tx.send(TrayEvent::Activate);
    }
}

pub fn start_tray_thread(tx: mpsc::Sender<TrayEvent>) {
    std::thread::spawn(move || {
        if let Err(e) = setup_tray(tx) {
            eprintln!("tray setup failed: {e}");
            log::warn!("tray: {}", e);
        }
    });
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

pub fn setup_tray_polling(
    tray_rx: mpsc::Receiver<TrayEvent>,
    window: libadwaita::ApplicationWindow,
) {
    glib::timeout_add_local(std::time::Duration::from_millis(200), move || {
        while let Ok(_) = tray_rx.try_recv() {
            if window.is_visible() {
                window.set_visible(false);
            } else {
                window.present();
            }
        }
        glib::ControlFlow::Continue
    });
}
