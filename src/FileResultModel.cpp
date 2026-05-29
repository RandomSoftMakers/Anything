#include "FileResultModel.h"

FileResultModel::FileResultModel(QObject* parent)
    : QAbstractListModel(parent) {}

int FileResultModel::rowCount(const QModelIndex& parent) const {
    if (parent.isValid()) return 0;
    return m_results.size();
}

QVariant FileResultModel::data(const QModelIndex& index, int role) const {
    if (!index.isValid() || index.row() >= m_results.size())
        return {};

    const auto& r = m_results.at(index.row());

    if (role == Qt::DisplayRole || role == NameRole)
        return r.name;
    if (role == FullPathRole)
        return r.fullPath;

    return {};
}

QHash<int, QByteArray> FileResultModel::roleNames() const {
    return {
        { NameRole,     "name" },
        { FullPathRole, "fullPath" },
    };
}

void FileResultModel::setResults(const QVector<FileResult>& results) {
    beginResetModel();
    m_results = results;
    endResetModel();
}
