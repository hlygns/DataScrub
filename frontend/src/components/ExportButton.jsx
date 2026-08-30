import { useState } from "react";
import { exportCleanedDataset } from "../services/api";

// Bu bileşenin tek görevi: kullanıcı butona bastığında temiz dosyayı
// backend'den çekip tarayıcıya "indir" olarak sunmak.
function ExportButton({ datasetId }) {
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState("");

  const handleExport = async () => {
    try {
      setExporting(true);
      setError("");

      const blob = await exportCleanedDataset(datasetId);

      // Backend'den gelen ham dosya verisini (blob), tarayıcının "indir" diyaloğuna
      // dönüştürüyoruz. Bu, sunucu tarafında bir dosya kaydetmeden, doğrudan
      // tarayıcı belleğinde geçici bir indirme linki oluşturup tıklatma tekniği.
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = "temizlenmis_veri.csv";
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (err) {
      console.error(err);
      setError("Export sırasında hata oluştu.");
    } finally {
      setExporting(false);
    }
  };

  return (
    <div>
      <h2>3. Temiz Dosyayı İndir</h2>
      <p>Onayladığın düzeltmeler uygulanmış temiz dosyayı indirebilirsin.</p>
      <button onClick={handleExport} disabled={exporting}>
        {exporting ? "Hazırlanıyor..." : "Temiz Dosyayı İndir"}
      </button>
      {error && <p style={{ color: "red" }}>{error}</p>}
    </div>
  );
}

export default ExportButton;
