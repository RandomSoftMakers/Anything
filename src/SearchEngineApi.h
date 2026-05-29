#pragma once

#include <QLibrary>
#include <QString>
#include <QVector>
#include <QDebug>

struct FileResult {
    QString name;
    QString fullPath;
};

class SearchEngineApi {
public:
    SearchEngineApi();
    ~SearchEngineApi();

    bool load(const QString& libPath);
    bool isLoaded() const { return m_lib.isLoaded(); }
    QString lastError() const { return m_lib.errorString(); }

    bool buildIndex(const QStringList& rootDirs);
    qint64 search(const QString& query, int searchType = 0);
    FileResult getResult(quint64 index) const;
    qint64 indexSize() const;
    qint64 lastResultsCount() const;

private:
    QLibrary m_lib;

    using BuildIndexFn         = int (*)(const char**, quint64);
    using SearchQueryFn        = quint64 (*)(const char*, int);
    using GetResultByIndexFn   = char* (*)(quint64);
    using FreeCStringFn        = void (*)(char*);
    using IndexSizeFn          = quint64 (*)();
    using LastResultsCountFn   = quint64 (*)();

    BuildIndexFn       m_buildIndex       = nullptr;
    SearchQueryFn      m_searchQuery      = nullptr;
    GetResultByIndexFn m_getResultByIndex = nullptr;
    FreeCStringFn      m_freeCString      = nullptr;
    IndexSizeFn        m_indexSize        = nullptr;
    LastResultsCountFn m_lastResultsCount = nullptr;
};
