import { useCallback, useEffect, useState } from "react";
import { getAllDatasets, describeError } from "../services/api";

// Daha önce yüklenmiş dosyaların listesi. Backend'de zaten GET /api/datasets vardı
// ama arayüzde karşılığı yoktu - sayfa yenilenince yapılan iş kaybolmuş gibi görünüyordu.
// Buradan eski bir dosyaya tıklayıp kaldığı yerden devam edilebiliyor.
function DatasetHistory({ activeDatasetId, onSelect, refreshKey }) {
  const [datasets, setDatasets] = useState([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    try {
      const data = await getAllDatasets();
      setDatasets(data);
      setError("");
    } catch (err) {
      console.error(err);
      setError(describeError(err));
    } finally {
      setLoading(false);
    }
  }, []);

  // refreshKey değişince (yeni yükleme ya da onay sonrası) liste tazeleniyor
  useEffect(() => {
    load();
  }, [load, refreshKey]);

  return (
    <aside className="section history">
      <h2>Geçmiş</h2>

      {loading && <p><span className="spinner" />Yükleniyor...</p>}
      {error && <p className="error-text">{error}</p>}
      {!loading && !error && datasets.length === 0 && (
        <p className="empty-state">Henüz dosya yüklenmemiş.</p>
      )}

      <ul className="history-list">
        {datasets.map((d) => (
          <li key={d.id}>
            <button
              type="button"
              className={`history-item ${d.id === activeDatasetId ? "is-active" : ""}`}
              onClick={() => onSelect(d.id)}
            >
              <span className="history-name" title={d.fileName}>{d.fileName}</span>
              <span className="history-meta">
                <span className={`badge status-${d.pendingIssues > 0 ? "pending" : "approved"}`}>
                  {d.pendingIssues > 0 ? `${d.pendingIssues} bekliyor` : "tamam"}
                </span>
                <span className="cell-muted">{d.totalIssues} sorun</span>
              </span>
            </button>
          </li>
        ))}
      </ul>
    </aside>
  );
}

export default DatasetHistory;
