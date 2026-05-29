#pragma once

#include <QAbstractListModel>
#include <QVector>
#include "SearchEngineApi.h"

class FileResultModel : public QAbstractListModel {
    Q_OBJECT
public:
    enum Roles {
        NameRole = Qt::UserRole + 1,
        FullPathRole,
    };

    explicit FileResultModel(QObject* parent = nullptr);

    int rowCount(const QModelIndex& parent = {}) const override;
    QVariant data(const QModelIndex& index, int role = Qt::DisplayRole) const override;
    QHash<int, QByteArray> roleNames() const override;

    void setResults(const QVector<FileResult>& results);

private:
    QVector<FileResult> m_results;
};
