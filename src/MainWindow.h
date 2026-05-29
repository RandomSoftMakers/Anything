#pragma once

#include <QMainWindow>
#include <QLineEdit>
#include <QListView>
#include <QLabel>
#include <QPushButton>
#include <QTimer>
#include <QThread>
#include <QMutex>
#include <QAtomicInt>

#include "SearchEngineApi.h"
#include "FileResultModel.h"

class MainWindow : public QMainWindow {
    Q_OBJECT
public:
    explicit MainWindow(QWidget* parent = nullptr);
    ~MainWindow() override;

protected:
    void showEvent(QShowEvent* event) override;

private slots:
    void onSearchTextChanged(const QString& text);
    void onDebounceTimeout();
    void onResultClicked(const QModelIndex& index);
    void onThemeToggle();
    void onAbout();

private:
    void setupUi();
    void runSearch(const QString& query);
    void applyMicaAndTheme(bool dark);
    void applyStyleSheetTheme(bool dark);
    void updateThemeButton();

    SearchEngineApi  m_engine;
    FileResultModel* m_model = nullptr;

    QLineEdit*       m_searchBox   = nullptr;
    QListView*       m_resultView  = nullptr;
    QLabel*          m_statusLabel = nullptr;
    QLabel*          m_filterHint  = nullptr;
    QPushButton*     m_themeBtn    = nullptr;
    QPushButton*     m_aboutBtn    = nullptr;
    QTimer*          m_debounce    = nullptr;
    QThread*         m_workerThread = nullptr;
    QAtomicInt       m_searchId{0};
    bool             m_isDark      = false;
    bool             m_compositionApplied = false;
};
