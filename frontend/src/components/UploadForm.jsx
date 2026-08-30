import { useState } from "react";
import { uploadDataset, analyzeDataset } from "../services/api";

// Bu bileşenin tek görevi: kullanıcıdan dosya almak, yükleyip analiz etmek,
// sonucu (datasetId) parent bileşene (App.jsx) haber vermek.
function UploadForm({ onAnalysisComplete }) {
  const [file, setFile] = useState(null);
  const [status, setStatus] = useState(""); // kullanıcıya "ne oluyor" bilgisi vermek için
  const [error, setError] = useState("");

  const handleFileChange = (event) => {
    setFile(event.target.files[0]);
    setError("");
  };

  const handleUpload = async () => {
    if (!file) {
      setError("Lütfen önce bir dosya seç.");
      return;
    }

    try {
      setError("");
      setStatus("Yükleniyor...");
      const uploadResult = await uploadDataset(file);

      setStatus("Analiz ediliyor... (birkaç saniye sürebilir)");
      await analyzeDataset(uploadResult.datasetId);

      setStatus("Tamamlandı!");
      onAnalysisComplete(uploadResult.datasetId);
    } catch (err) {
      console.error(err);
      setError("Bir hata oluştu: " + (err.response?.data || err.message));
      setStatus("");
    }
  };

  return (
    <div>
      <h2>1. Dosya Yükle</h2>
      <input type="file" accept=".csv,.xlsx" onChange={handleFileChange} />
      <button onClick={handleUpload} disabled={!file || status === "Yükleniyor..." || status === "Analiz ediliyor... (birkaç saniye sürebilir)"}>
        Yükle ve Analiz Et
      </button>

      {status && <p>{status}</p>}
      {error && <p style={{ color: "red" }}>{error}</p>}
    </div>
  );
}

export default UploadForm;