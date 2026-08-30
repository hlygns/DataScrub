import { useEffect, useState } from "react";
import { getDatasetSummary, resolveIssue } from "../services/api";

// Bu bileşen: bir datasetId alır, o dataset'in sorunlarını gösterir,
// her sorun için onayla/reddet butonları sunar.
function IssuesList({ datasetId }) {
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);

  // useEffect: bileşen ekrana ilk geldiğinde (ya da datasetId değiştiğinde) çalışır.
  // C#'taki constructor'a benzetebilirsin ama "veri her değiştiğinde tekrar çalış" mantığıyla.
  useEffect(() => {
    loadSummary();
  }, [datasetId]);

  const loadSummary = async () => {
    setLoading(true);
    const data = await getDatasetSummary(datasetId);
    setSummary(data);
    setLoading(false);
  };

  const handleResolve = async (issueId, approve) => {
    await resolveIssue(issueId, approve);
    await loadSummary(); // onayladıktan sonra listeyi tazele, güncel durumu göster
  };

  if (loading) return <p>Yükleniyor...</p>;
  if (!summary) return null;

  return (
    <div>
      <h2>2. Tespit Edilen Sorunlar</h2>
      <p>
        Toplam: {summary.totalIssues} | Bekleyen: {summary.pendingIssues} | Çözülen: {summary.resolvedIssues}
      </p>

      <table border="1" cellPadding="8" style={{ borderCollapse: "collapse", width: "100%" }}>
        <thead>
          <tr>
            <th>Tip</th>
            <th>Satır</th>
            <th>Kolon</th>
            <th>Orijinal</th>
            <th>Önerilen</th>
            <th>Güven</th>
            <th>Durum</th>
            <th>Aksiyon</th>
          </tr>
        </thead>
        <tbody>
          {summary.issues.map((issue) => (
            <tr key={issue.id}>
              <td>{issue.type}</td>
              <td>{issue.rowIndex}</td>
              <td>{issue.columnName}</td>
              <td>{issue.originalValue ?? "-"}</td>
              <td>{issue.suggestedValue ?? "-"}</td>
              <td>{(issue.confidenceScore * 100).toFixed(0)}%</td>
              <td>{issue.resolution}</td>
              <td>
                {issue.resolution === "Pending" && (
                  <>
                    <button onClick={() => handleResolve(issue.id, true)}>Onayla</button>{" "}
                    <button onClick={() => handleResolve(issue.id, false)}>Reddet</button>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default IssuesList;