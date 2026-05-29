#include "MainWindow.h"

#include <QVBoxLayout>
#include <QHBoxLayout>
#include <QHeaderView>
#include <QApplication>
#include <QDesktopServices>
#include <QUrl>
#include <QFileInfo>
#include <QDebug>
#include <QDir>
#include <QStandardPaths>
#include <QMessageBox>
#include <QShowEvent>

#include <QWindow>

#ifdef Q_OS_WIN
#include <windows.h>
#include <dwmapi.h>
#endif

// ── Mica + DWM ───────────────────────────────────────────────────────

void MainWindow::applyMicaAndTheme(bool dark) {
#ifdef Q_OS_WIN
    QWindow* win = windowHandle();
    if (!win) return;
    HWND hwnd = reinterpret_cast<HWND>(win->winId());
    if (!hwnd) return;

    // Mica backdrop (Win11 22H2+: DWMWA_SYSTEMBACKDROP_TYPE = 38)
    int backdropType = 4; // DWMSBT_THUMBNAIL = Mica Alt
    HRESULT hr = DwmSetWindowAttribute(hwnd, 38, &backdropType, sizeof(backdropType));
    if (FAILED(hr)) {
        // Fallback: original Win11 Mica (DWMWA_MICA_ENABLED = 1029)
        BOOL mica = TRUE;
        DwmSetWindowAttribute(hwnd, 1029, &mica, sizeof(mica));
    }

    // DWM dark/light title bar
    BOOL useDark = dark ? TRUE : FALSE;
    DwmSetWindowAttribute(hwnd, 20, &useDark, sizeof(useDark));
#endif
}

// ── Stylesheets ──────────────────────────────────────────────────────

static const char* LIGHT_STYLE = R"(
    QMainWindow { background-color: #F2F2F2; }
    QLabel#titleLabel { color: #1A1A1A; font-size: 28px; font-weight: 600; }
    QLabel#statusLabel { color: #707070; font-size: 12px; }
    QLabel#filterHint { color: #707070; font-size: 11px; }
    QLineEdit {
        background-color: #FFFFFF;
        color: #1A1A1A;
        border: 1px solid #D1D1D1;
        border-radius: 6px;
        padding: 8px 12px;
        font-size: 16px;
        selection-background-color: #005FB8;
    }
    QLineEdit:focus { border: 1.5px solid #005FB8; }
    QListView {
        background-color: #FFFFFF;
        color: #1A1A1A;
        border: 1px solid #E5E5E5;
        outline: none;
    }
    QListView::item {
        height: 36px;
        padding: 0 8px;
    }
    QListView::item:selected { background-color: #CCE0F5; color: #1A1A1A; }
    QListView::item:hover:!selected { background-color: #E5F0FA; }
    QPushButton#toolBtn {
        background: transparent;
        border: none;
        padding: 4px 8px;
        font-size: 18px;
    }
    QPushButton#toolBtn:hover { background-color: rgba(0,0,0,0.05); border-radius: 4px; }
)";

static const char* DARK_STYLE = R"(
    QMainWindow { background-color: #1E1E1E; }
    QLabel#titleLabel { color: #E0E0E0; font-size: 28px; font-weight: 600; }
    QLabel#statusLabel { color: #999999; font-size: 12px; }
    QLabel#filterHint { color: #999999; font-size: 11px; }
    QLineEdit {
        background-color: #2D2D2D;
        color: #E0E0E0;
        border: 1px solid #3D3D3D;
        border-radius: 6px;
        padding: 8px 12px;
        font-size: 16px;
        selection-background-color: #4BA3F5;
    }
    QLineEdit:focus { border: 1.5px solid #4BA3F5; }
    QListView {
        background-color: #2D2D2D;
        color: #E0E0E0;
        border: 1px solid #3D3D3D;
        outline: none;
    }
    QListView::item {
        height: 36px;
        padding: 0 8px;
    }
    QListView::item:selected { background-color: #333348; color: #E0E0E0; }
    QListView::item:hover:!selected { background-color: #2A2A3C; }
    QPushButton#toolBtn {
        background: transparent;
        border: none;
        padding: 4px 8px;
        font-size: 18px;
    }
    QPushButton#toolBtn:hover { background-color: rgba(255,255,255,0.1); border-radius: 4px; }
)";

// ── Конструктор / Деструктор ─────────────────────────────────────────

MainWindow::MainWindow(QWidget* parent)
    : QMainWindow(parent)
{
    setWindowTitle("Anything — быстрый поиск файлов");
    resize(960, 640);
    setMinimumSize(600, 400);

    setupUi();

    // Light theme by default
    applyStyleSheetTheme(false);

    // Загружаем Rust-библиотеку (QLibrary учитывает платформу:
    // Windows → searchengine.dll, Linux → libsearchengine.so, macOS → libsearchengine.dylib)
    QString libPath = QApplication::applicationDirPath() + "/searchengine";
    if (!m_engine.load(libPath)) {
        m_statusLabel->setText("Ошибка: не удалось загрузить searchengine (" + m_engine.lastError() + ")");
        return;
    }

    // Собираем корневые каталоги
    QStringList roots;
    const auto dirs = {
        QStandardPaths::writableLocation(QStandardPaths::DesktopLocation),
        QStandardPaths::writableLocation(QStandardPaths::DocumentsLocation),
        QStandardPaths::writableLocation(QStandardPaths::DownloadLocation),
        QStandardPaths::writableLocation(QStandardPaths::PicturesLocation),
        QStandardPaths::writableLocation(QStandardPaths::MusicLocation),
        QStandardPaths::writableLocation(QStandardPaths::MoviesLocation),
    };
    for (const auto& d : dirs) {
        if (QDir(d).exists())
            roots.append(d);
    }

    m_statusLabel->setText("Индексация файловой системы...");

    if (m_engine.buildIndex(roots)) {
        qint64 size = m_engine.indexSize();
        m_statusLabel->setText(QString("Готов к поиску (индекс: %1 записей)").arg(size));
    } else {
        m_statusLabel->setText("Ошибка: не удалось построить индекс");
    }
}

MainWindow::~MainWindow() {}

void MainWindow::showEvent(QShowEvent* event) {
    QMainWindow::showEvent(event);
    if (!m_compositionApplied) {
        m_compositionApplied = true;
        applyMicaAndTheme(m_isDark);
    }
}

// ── UI ───────────────────────────────────────────────────────────────

void MainWindow::setupUi() {
    auto* central = new QWidget(this);
    auto* layout = new QVBoxLayout(central);
    layout->setContentsMargins(32, 8, 32, 16);

    // Title row
    auto* titleRow = new QHBoxLayout();

    auto* title = new QLabel("Anything", this);
    title->setObjectName("titleLabel");
    titleRow->addWidget(title);

    titleRow->addStretch();

    m_themeBtn = new QPushButton("🌙", this);
    m_themeBtn->setObjectName("toolBtn");
    m_themeBtn->setToolTip("Переключить тему");
    titleRow->addWidget(m_themeBtn);

    m_aboutBtn = new QPushButton("ⓘ", this);
    m_aboutBtn->setObjectName("toolBtn");
    m_aboutBtn->setToolTip("О программе");
    titleRow->addWidget(m_aboutBtn);

    layout->addLayout(titleRow);

    // Filter hint
    m_filterHint = new QLabel(this);
    m_filterHint->setObjectName("filterHint");
    m_filterHint->setVisible(false);
    layout->addWidget(m_filterHint);

    // Search box
    m_searchBox = new QLineEdit(this);
    m_searchBox->setPlaceholderText("Введите запрос (например: !tmp ext:pdf \"отчёт\")");
    m_searchBox->setClearButtonEnabled(true);
    m_searchBox->setMinimumHeight(36);
    layout->addWidget(m_searchBox);

    // Result list
    m_model = new FileResultModel(this);
    m_resultView = new QListView(this);
    m_resultView->setModel(m_model);
    m_resultView->setSelectionMode(QAbstractItemView::SingleSelection);
    m_resultView->setEditTriggers(QAbstractItemView::NoEditTriggers);
    layout->addWidget(m_resultView, 1);

    // Status bar
    m_statusLabel = new QLabel("Готов к поиску", this);
    m_statusLabel->setObjectName("statusLabel");
    layout->addWidget(m_statusLabel);

    setCentralWidget(central);

    // Debounce
    m_debounce = new QTimer(this);
    m_debounce->setSingleShot(true);
    m_debounce->setInterval(300);

    connect(m_searchBox, &QLineEdit::textChanged, this, &MainWindow::onSearchTextChanged);
    connect(m_debounce, &QTimer::timeout, this, &MainWindow::onDebounceTimeout);
    connect(m_resultView, &QListView::clicked, this, &MainWindow::onResultClicked);
    connect(m_themeBtn, &QPushButton::clicked, this, &MainWindow::onThemeToggle);
    connect(m_aboutBtn, &QPushButton::clicked, this, &MainWindow::onAbout);
}

// ── Theme ────────────────────────────────────────────────────────────

void MainWindow::applyStyleSheetTheme(bool dark) {
    m_isDark = dark;
    qApp->setStyleSheet(dark ? DARK_STYLE : LIGHT_STYLE);
    updateThemeButton();
}

void MainWindow::updateThemeButton() {
    m_themeBtn->setText(m_isDark ? "☀️" : "🌙");
}

void MainWindow::onThemeToggle() {
    m_isDark = !m_isDark;
    applyStyleSheetTheme(m_isDark);
    applyMicaAndTheme(m_isDark);
}

void MainWindow::onAbout() {
    QMessageBox about(this);
    about.setWindowTitle("О программе Anything");
    about.setTextFormat(Qt::RichText);
    about.setText(
        "<h2>Anything</h2>"
        "<p>Быстрый поиск файлов</p>"
        "<p><b>Версия:</b> 2.0<br>"
        "<b>Движок:</b> Rust (LibAnything + SearchEngine)<br>"
        "<b>GUI:</b> Qt 6 + MinGW<br>"
        "<p>Архитектура: LibAnything (сканирование ФС)<br>"
        "→ SearchEngine (нечёткий поиск + FFI)<br>"
        "→ GUI (Qt 6)</p>"
        "<p><b>Синтаксис поиска:</b><br>"
        "&nbsp;&nbsp;!терм — исключить<br>"
        "&nbsp;&nbsp;\"фраза\" — точное совпадение<br>"
        "&nbsp;&nbsp;ext:pdf — только PDF<br>"
        "&nbsp;&nbsp;ext:!tmp — кроме TMP<br>"
        "&nbsp;&nbsp;path:C:\\Docs — путь</p>"
        "<p style='color: gray; font-size: 11px;'>© 2026 Anything Team. GPL V3 License.</p>"
    );
    about.exec();
}

// ── Search ───────────────────────────────────────────────────────────

void MainWindow::onSearchTextChanged(const QString& text) {
    m_debounce->start();
}

void MainWindow::onDebounceTimeout() {
    QString query = m_searchBox->text().trimmed();
    if (query.isEmpty()) {
        m_model->setResults({});
        m_filterHint->setVisible(false);
        qint64 size = m_engine.indexSize();
        m_statusLabel->setText(QString("Готов к поиску (индекс: %1 записей)").arg(size));
        return;
    }
    runSearch(query);
}

void MainWindow::runSearch(const QString& query) {
    int searchId = ++m_searchId;

    qint64 count = m_engine.search(query);

    if (count == 0) {
        m_model->setResults({});
        m_filterHint->setVisible(false);
        m_statusLabel->setText("Совпадений не найдено");
        return;
    }

    QVector<FileResult> results;
    results.reserve(static_cast<int>(count));
    for (quint64 i = 0; i < static_cast<quint64>(count); ++i) {
        auto fr = m_engine.getResult(i);
        if (!fr.fullPath.isEmpty())
            results.append(std::move(fr));
    }

    m_model->setResults(results);
    m_statusLabel->setText(QString("Найдено: %1 совпадений").arg(results.size()));
}

void MainWindow::onResultClicked(const QModelIndex& index) {
    if (!index.isValid()) return;
    QString path = m_model->data(index, FileResultModel::FullPathRole).toString();
    if (!path.isEmpty()) {
        QDesktopServices::openUrl(QUrl::fromLocalFile(QFileInfo(path).absolutePath()));
    }
}
