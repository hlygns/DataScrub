import { useState } from "react";
import { exportCleanedDataset, describeError } from "../services/api";

// Bu bileşenin tek görevi: kullanıcı butona bastığında temiz dosyayı
// backend'den çekip tarayıcıya "indir" olarak sunmak.
function ExportButton({ datasetId, approvedCount = 0 }) {
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState("");
  const [downloaded, setDownloaded] = useState("");

  const handleExport = async () => {
    try {
      setExporting(true);
      setError("");
      setDownloaded("");

      const { blob, fileName } = await exportCleanedDataset(datasetId);

      // Backend'den gelen ham dosya verisini (blob), tarayıcının "indir" diyaloğuna
      // dönüştürüyoruz. Bu, sunucu tarafında bir dosya kaydetmeden, doğrudan
      // tarayıcı belleğinde geçici bir indirme linki oluşturup tıklatma tekniği.
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName; // adı sunucu veriyor, uzantı korunuyor (.csv / .xlsx)
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

      setDownloaded(fileName);
    } catch (err) {
      console.error(err);
      setError(describeError(err));
    } finally {
      setExporting(false);
    }
  };

  return (
    <section className="section">
      <h2>3. Temiz Dosyayı İndir</h2>
      <p>
        Sadece <strong>onayladığın</strong> düzeltmeler uygulanır. Reddettiklerin ve karar
        vermediklerin orijinal hâliyle kalır.
      </p>

      {approvedCount === 0 && (
        <p className="hint">
          Henüz onaylanmış bir öneri yok — dosyayı şimdi indirirsen orijinaliyle aynı olur.
        </p>
      )}

      <button onClick={handleExport} disabled={exporting}>
        {exporting && <span className="spinner" />}
        {exporting ? "Hazırlanıyor..." : `Temiz Dosyayı İndir${approvedCount > 0 ? ` (${approvedCount} düzeltme)` : ""}`}
      </button>

      {downloaded && <p className="success-text">İndirildi: {downloaded}</p>}
      {error && <p className="error-text">{error}</p>}
    </section>
  );
}

export default ExportButton;
