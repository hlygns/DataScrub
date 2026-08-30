import { useState } from "react";
import UploadForm from "./components/UploadForm";
import IssuesList from "./components/IssuesList";
import ExportButton from "./components/ExportButton";

// Ana bileşen: hangi adımda olduğumuzu (datasetId var mı yok mu) yönetiyor,
// alt bileşenleri (UploadForm, IssuesList, ExportButton) sırayla gösteriyor.
function App() {
  const [datasetId, setDatasetId] = useState(null);

  return (
    <div style={{ maxWidth: "900px", margin: "40px auto", padding: "30px", background: "#1e293b", borderRadius: "12px" }}>
      <h1>DataScrub — Otonom Veri Temizleme</h1>

      <UploadForm onAnalysisComplete={setDatasetId} />

      {datasetId && (
        <>
          <hr />
          <IssuesList datasetId={datasetId} />
          <hr />
          <ExportButton datasetId={datasetId} />
        </>
      )}
    </div>
  );
}

export default App;