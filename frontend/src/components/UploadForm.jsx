import { useState } from "react";
import { uploadDataset, analyzeDataset, describeError } from "../services/api";

// Bu bileşenin tek görevi: kullanıcıdan dosya almak, yükleyip analiz etmek,
// sonucu (datasetId) parent bileşene (App.jsx) haber vermek.
function UploadForm({ onAnalysisComplete }) {
  const [file, setFile] = useState(null);
  const [phase, setPhase] = useState("idle"); // idle | uploading | analyzing | done
  const [error, setError] = useState("");

  const busy = phase === "uploading" || phase === "analyzing";

  const handleFileChange = (event) => {
    setFile(event.target.files[0] ?? null);
    setError("");
    setPhase("idle");
  };

  const handleUpload = async () => {
    if (!file) {
      setError("Lütfen önce bir dosya seç.");
      return;
    }

    try {
      setError("");
      setPhase("uploading");
      const uploadResult = await uploadDataset(file);

      setPhase("analyzing");
      await analyzeDataset(uploadResult.datasetId);

      setPhase("done");
      onAnalysisComplete(uploadResult.datasetId);
    } catch (err) {
      console.error(err);
      setError(describeError(err));
      setPhase("idle");
    }
  };

  return (
    <section className="section">
      <h2>1. Dosya Yükle</h2>

      <div className="upload-row">
        <label className="file-input">
          <input type="file" accept=".csv,.xlsx" onChange={handleFileChange} disabled={busy} />
          <span>{file ? file.name : "Dosya seç (.csv veya .xlsx)"}</span>
        </label>

        <button onClick={handleUpload} disabled={!file || busy}>
          {busy && <span className="spinner" />}
          {phase === "uploading" && "Yükleniyor..."}
          {phase === "analyzing" && "Analiz ediliyor..."}
          {!busy && "Yükle ve Analiz Et"}
        </button>
      </div>

      {phase === "analyzing" && (
        <p className="hint">Mükerrer kayıt, eksik veri ve format kontrolleri paralel çalışıyor — birkaç saniye sürebilir.</p>
      )}
      {phase === "done" && <p className="success-text">Analiz tamamlandı.</p>}
      {error && <p className="error-text">{error}</p>}
    </section>
  );
}

export default UploadForm;
