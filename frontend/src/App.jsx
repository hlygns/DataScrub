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
        <h1>DataScrub</h1>
        <p>
          Otonom veri temizleme — mükerrer kayıt, eksik veri ve format hatalarını tespit eder,
          <strong> ama sen onaylamadan hiçbir şeyi değiştirmez.</strong>
        </p>
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
