import { useCallback, useEffect, useMemo, useState } from "react";
import { getDatasetSummary, resolveIssue, resolveIssuesBulk, describeError } from "../services/api";

const PAGE_SIZE = 25;

// Backend enum'larının Türkçe karşılıkları. Backend "Duplicate" döner, kullanıcı
// "Mükerrer Kayıt" görür - çeviriyi tek yerde tutuyoruz ki her tabloda tekrarlamayalım.
const TYPE_LABELS = {
  Duplicate: "Mükerrer Kayıt",
  MissingValue: "Eksik Veri",
  FormatError: "Format Hatası",
};

const STATUS_LABELS = {
  Pending: "Bekliyor",
  Approved: "Onaylandı",
  Rejected: "Reddedildi",
};

// Güven skoruna göre renk: yüksekse yeşil, ortaysa sarı, düşükse kırmızı.
// Kullanıcının "hangisine güvenebilirim" sorusunu tabloya bakar bakmaz yanıtlıyor.
const confidenceColor = (score) => {
  if (score >= 0.8) return "var(--conf-high)";
  if (score >= 0.6) return "var(--conf-mid)";
  return "var(--conf-low)";
};

// Backend satırları 0'dan başlatıyor (veri çerçevesi indeksi). Kullanıcı ise dosyayı
// Excel'de açıyor: başlık 1. satır, ilk veri satırı 2. satır. Ekranda Excel numarasını
// gösteriyoruz ki "Satır 5" dediğimizde kullanıcı aynı satırı bulsun.
const displayRow = (rowIndex) => rowIndex + 2;

function IssuesList({ datasetId, onSummaryChange, onResolved }) {
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const [typeFilter, setTypeFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("Pending"); // varsayılan: iş bekleyenler
  const [minConfidence, setMinConfidence] = useState(0);
  const [page, setPage] = useState(1);

  const loadSummary = useCallback(async () => {
    try {
      const data = await getDatasetSummary(datasetId);
      setSummary(data);
      setError("");
      onSummaryChange?.(data); // App export butonunun "kaç düzeltme" sayısını buradan alıyor
    } catch (err) {
      console.error(err);
      setError(describeError(err));
    } finally {
      setLoading(false);
    }
  }, [datasetId, onSummaryChange]);

  // App bu bileşene key={datasetId} veriyor, yani dataset değişince bileşen
  // baştan mount oluyor - filtreleri/sayfayı burada elle sıfırlamaya gerek yok.
  useEffect(() => {
    loadSummary();
  }, [loadSummary]);

  const issues = useMemo(() => summary?.issues ?? [], [summary]);

  // Filtreleme her render'da değil, sadece girdiler değişince hesaplanıyor (useMemo).
  // Binlerce issue'da her tuş vuruşunda yeniden filtrelemeyi önlüyor.
  const filteredIssues = useMemo(
    () =>
      issues.filter((issue) => {
        if (typeFilter !== "all" && issue.type !== typeFilter) return false;
        if (statusFilter !== "all" && issue.resolution !== statusFilter) return false;
        if (issue.confidenceScore * 100 < minConfidence) return false;
        return true;
      }),
    [issues, typeFilter, statusFilter, minConfidence]
  );

  const totalPages = Math.max(1, Math.ceil(filteredIssues.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const visibleIssues = filteredIssues.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  // Toplu aksiyon sadece "Bekliyor" durumundakilere uygulanır - zaten karara
  // bağlanmış bir öneriyi yanlışlıkla geri çevirmeyi önlüyoruz.
  const pendingInFilter = filteredIssues.filter((i) => i.resolution === "Pending");

  const refresh = async () => {
    await loadSummary();
    onResolved?.();
  };

  const handleResolve = async (issueId, approve) => {
    try {
      setBusy(true);
      await resolveIssue(issueId, approve);
      await refresh();
    } catch (err) {
      console.error(err);
      setError(describeError(err));
    } finally {
      setBusy(false);
    }
  };

  const handleBulk = async (targetIssues, approve) => {
    if (targetIssues.length === 0) return;

    const action = approve ? "onaylamak" : "reddetmek";
    if (!window.confirm(`${targetIssues.length} öneriyi ${action} istediğine emin misin?`)) return;

    try {
      setBusy(true);
      await resolveIssuesBulk(targetIssues.map((i) => i.id), approve);
      await refresh();
    } catch (err) {
      console.error(err);
      setError(describeError(err));
    } finally {
      setBusy(false);
    }
  };

  if (loading) {
    return (
      <section className="section">
        <h2>2. Tespit Edilen Sorunlar</h2>
        <p><span className="spinner" />Yükleniyor...</p>
      </section>
    );
  }

  if (!summary) return null;

  return (
    <section className="section">
      <h2>2. Tespit Edilen Sorunlar</h2>

      <div className="stats-row">
        <span className="stat-pill">Dosya: <strong>{summary.fileName}</strong></span>
        <span className="stat-pill">Satır: <strong>{summary.rowCount}</strong></span>
        <span className="stat-pill">Toplam: <strong>{summary.totalIssues}</strong></span>
        <span className="stat-pill">Bekleyen: <strong>{summary.pendingIssues}</strong></span>
        <span className="stat-pill">Çözülen: <strong>{summary.resolvedIssues}</strong></span>
      </div>

      {summary.totalIssues === 0 ? (
        <p className="empty-state">Bu dosyada herhangi bir veri kalitesi sorunu bulunamadı.</p>
      ) : (
        <>
          <div className="filter-bar">
            <label>
              Tip
              <select value={typeFilter} onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}>
                <option value="all">Hepsi</option>
                <option value="Duplicate">Mükerrer Kayıt</option>
                <option value="MissingValue">Eksik Veri</option>
                <option value="FormatError">Format Hatası</option>
              </select>
            </label>

            <label>
              Durum
              <select value={statusFilter} onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}>
                <option value="Pending">Bekleyenler</option>
                <option value="Approved">Onaylananlar</option>
                <option value="Rejected">Reddedilenler</option>
                <option value="all">Hepsi</option>
              </select>
            </label>

            <label className="range-label">
              Min. güven: <strong>%{minConfidence}</strong>
              <input
                type="range"
                min="0"
                max="100"
                step="5"
                value={minConfidence}
                onChange={(e) => { setMinConfidence(Number(e.target.value)); setPage(1); }}
              />
            </label>
          </div>

          <div className="bulk-bar">
            <span className="bulk-info">
              {filteredIssues.length} sonuç gösteriliyor
              {pendingInFilter.length > 0 && ` · ${pendingInFilter.length} tanesi karar bekliyor`}
            </span>
            <div className="bulk-actions">
              <button
                onClick={() => handleBulk(pendingInFilter, true)}
                disabled={busy || pendingInFilter.length === 0}
              >
                Filtrelenenleri Onayla ({pendingInFilter.length})
              </button>
              <button
                className="secondary"
                onClick={() => handleBulk(pendingInFilter, false)}
                disabled={busy || pendingInFilter.length === 0}
              >
                Filtrelenenleri Reddet
              </button>
            </div>
          </div>

          {error && <p className="error-text">{error}</p>}

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Tip</th>
                  <th title="Dosyada Excel/CSV'de göründüğü satır numarası (başlık satırı = 1)">Satır</th>
                  <th>Kolon</th>
                  <th>Orijinal</th>
                  <th>Önerilen</th>
                  <th>Güven</th>
                  <th>Durum</th>
                  <th className="col-actions">Aksiyon</th>
                </tr>
              </thead>
              <tbody>
                {visibleIssues.map((issue) => (
                  <tr key={issue.id}>
                    <td>
                      <span className={`badge badge-${issue.type.toLowerCase()}`}>
                        {TYPE_LABELS[issue.type] ?? issue.type}
                      </span>
                    </td>
                    <td>
                      {displayRow(issue.rowIndex)}
                      {issue.relatedRowIndex != null && ` ↔ ${displayRow(issue.relatedRowIndex)}`}
                    </td>
                    <td className="cell-muted">{issue.columnName}</td>
                    <td className="cell-value" title={issue.originalValue ?? ""}>
                      {issue.originalValue || <span className="cell-muted">(boş)</span>}
                    </td>
                    <td className="cell-value" title={issue.suggestedValue ?? ""}>
                      {issue.suggestedValue ?? "-"}
                    </td>
                    <td>
                      <div className="confidence-bar-wrap">
                        <div className="confidence-bar">
                          <div
                            className="confidence-bar-fill"
                            style={{
                              width: `${issue.confidenceScore * 100}%`,
                              background: confidenceColor(issue.confidenceScore),
                            }}
                          />
                        </div>
                        <span>{(issue.confidenceScore * 100).toFixed(0)}%</span>
                      </div>
                    </td>
                    <td>
                      <span className={`badge status-${issue.resolution.toLowerCase()}`}>
                        {STATUS_LABELS[issue.resolution] ?? issue.resolution}
                      </span>
                    </td>
                    <td className="col-actions">
                      {issue.resolution === "Pending" && (
                        <div className="row-actions">
                          <button onClick={() => handleResolve(issue.id, true)} disabled={busy}>
                            Onayla
                          </button>
                          <button className="secondary" onClick={() => handleResolve(issue.id, false)} disabled={busy}>
                            Reddet
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {filteredIssues.length === 0 && (
            <p className="empty-state">Bu filtrelerle eşleşen öneri yok.</p>
          )}

          {totalPages > 1 && (
            <div className="pagination">
              <button onClick={() => setPage(safePage - 1)} disabled={safePage === 1}>
                ← Önceki
              </button>
              <span>Sayfa {safePage} / {totalPages}</span>
              <button onClick={() => setPage(safePage + 1)} disabled={safePage === totalPages}>
                Sonraki →
              </button>
            </div>
          )}
        </>
      )}
    </section>
  );
}

export default IssuesList;
