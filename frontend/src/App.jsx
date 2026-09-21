import { useCallback, useEffect, useState } from "react";
import UploadForm from "./components/UploadForm";
import IssuesList from "./components/IssuesList";
import ExportButton from "./components/ExportButton";
import DatasetHistory from "./components/DatasetHistory";

const ACTIVE_DATASET_KEY = "datascrub.activeDatasetId";

// Ana bileşen: hangi dataset üzerinde çalışıldığını yönetir, alt bileşenleri
// (geçmiş, yükleme, sorun listesi, export) bir araya getirir.
function App() {
  // Aktif dataset'i localStorage'da tutuyoruz - sayfa yenilendiğinde kullanıcının
  // yaptığı onaylar kaybolmuş gibi görünmesin diye.
  const [datasetId, setDatasetId] = useState(() => localStorage.getItem(ACTIVE_DATASET_KEY));
  const [summary, setSummary] = useState(null);
  const [historyKey, setHistoryKey] = useState(0);

  useEffect(() => {
    if (datasetId) localStorage.setItem(ACTIVE_DATASET_KEY, datasetId);
    else localStorage.removeItem(ACTIVE_DATASET_KEY);
  }, [datasetId]);

  const selectDataset = (id) => {
    setSummary(null);
    setDatasetId(id);
  };

  const handleUploadComplete = (id) => {
    selectDataset(id);
    setHistoryKey((k) => k + 1); // yeni dosya geçmiş listesine düşsün
  };

  // useCallback şart: IssuesList bunu useCallback bağımlılığında kullanıyor,
  // her render'da yeni bir fonksiyon versek sonsuz yeniden yükleme döngüsü olurdu.
  const handleSummaryChange = useCallback((data) => setSummary(data), []);
  const handleResolved = useCallback(() => setHistoryKey((k) => k + 1), []);

  const approvedCount = summary?.issues?.filter((i) => i.resolution === "Approved").length ?? 0;

  return (
    <div className="app">
      <header className="app-header">
        <div className="brand">
          <div className="brand-mark" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor"
                 strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M4 6h10M4 12h6M4 18h10" />
              <path d="M14.5 15.5l3 3 5-6" />
            </svg>
          </div>
          <div>
            <h1>Data<span className="accent">Scrub</span></h1>
            <p className="brand-sub">Otonom veri temizleme</p>
          </div>
        </div>

        <p className="tagline">
          Yüklediğin dosyadaki veri kalitesi sorunlarını tespit eder ve düzeltme önerir.{" "}
          <strong>Sen onaylamadan hiçbir şeyi değiştirmez.</strong>
        </p>

        <ul className="feature-chips">
          <li><span className="dot dot-duplicate" />Mükerrer kayıt</li>
          <li><span className="dot dot-missing" />Eksik veri</li>
          <li><span className="dot dot-format" />Format hatası</li>
          <li><span className="dot dot-approved" />İnsan onaylı</li>
        </ul>
      </header>

      <div className="app-layout">
        <DatasetHistory
          activeDatasetId={datasetId}
          onSelect={selectDataset}
          refreshKey={historyKey}
        />

        <main className="app-content">
          <UploadForm onAnalysisComplete={handleUploadComplete} />

          {datasetId ? (
            <>
              <IssuesList
                key={datasetId}
                datasetId={datasetId}
                onSummaryChange={handleSummaryChange}
                onResolved={handleResolved}
              />
              <ExportButton datasetId={datasetId} approvedCount={approvedCount} />
            </>
          ) : (
            <section className="section">
              <p className="empty-state">
                Başlamak için bir dosya yükle ya da soldaki geçmişten birini seç.
              </p>
            </section>
          )}
        </main>
      </div>
    </div>
  );
}

export default App;
