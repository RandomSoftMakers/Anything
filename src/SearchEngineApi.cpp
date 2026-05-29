#include "SearchEngineApi.h"
#include <QFileInfo>
#include <cstdlib>
#include <cstring>

SearchEngineApi::SearchEngineApi() {}

SearchEngineApi::~SearchEngineApi() {
    if (m_lib.isLoaded())
        m_lib.unload();
}

bool SearchEngineApi::load(const QString& libPath) {
    m_lib.setFileName(libPath);
    if (!m_lib.load()) {
        qWarning() << "Failed to load searchengine:" << m_lib.errorString();
        return false;
    }

    m_buildIndex       = reinterpret_cast<BuildIndexFn>(m_lib.resolve("build_index"));
    m_searchQuery      = reinterpret_cast<SearchQueryFn>(m_lib.resolve("search_query"));
    m_getResultByIndex = reinterpret_cast<GetResultByIndexFn>(m_lib.resolve("get_result_by_index"));
    m_freeCString      = reinterpret_cast<FreeCStringFn>(m_lib.resolve("free_c_string"));
    m_indexSize        = reinterpret_cast<IndexSizeFn>(m_lib.resolve("index_size"));
    m_lastResultsCount = reinterpret_cast<LastResultsCountFn>(m_lib.resolve("last_results_count"));

    if (!m_buildIndex || !m_searchQuery || !m_getResultByIndex || !m_freeCString ||
        !m_indexSize || !m_lastResultsCount) {
        qWarning() << "Failed to resolve one or more functions from searchengine";
        m_lib.unload();
        return false;
    }

    return true;
}

bool SearchEngineApi::buildIndex(const QStringList& rootDirs) {
    if (!m_buildIndex) return false;

    QVector<const char*> ptrs;
    QVector<QByteArray> storage;

    for (const auto& dir : rootDirs) {
        storage.append(dir.toUtf8() + '\0');
        ptrs.append(storage.last().constData());
    }

    int rc = m_buildIndex(ptrs.data(), ptrs.size());
    return rc == 0;
}

qint64 SearchEngineApi::search(const QString& query, int searchType) {
    if (!m_searchQuery) return -1;
    QByteArray utf8 = query.toUtf8() + '\0';
    quint64 count = m_searchQuery(utf8.constData(), searchType);
    return static_cast<qint64>(count);
}

FileResult SearchEngineApi::getResult(quint64 index) const {
    FileResult result;
    if (!m_getResultByIndex) return result;

    char* ptr = m_getResultByIndex(index);
    if (!ptr) return result;

    result.fullPath = QString::fromUtf8(ptr);
    result.name = QFileInfo(result.fullPath).fileName();

    if (m_freeCString)
        m_freeCString(ptr);

    return result;
}

qint64 SearchEngineApi::indexSize() const {
    if (!m_indexSize) return -1;
    return static_cast<qint64>(m_indexSize());
}

qint64 SearchEngineApi::lastResultsCount() const {
    if (!m_lastResultsCount) return -1;
    return static_cast<qint64>(m_lastResultsCount());
}
