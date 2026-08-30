import axios from "axios";

// Backend'in adresi - .NET tarafı 5000 portunda çalışıyor
const API_BASE_URL = "http://localhost:5000/api";

const api = axios.create({
  baseURL: API_BASE_URL,
});

// Dosya yükleme - multipart/form-data olarak gönderiyoruz, C# tarafındaki
// [HttpPost("upload")] IFormFile file parametresiyle eşleşiyor
export const uploadDataset = async (file) => {
  const formData = new FormData();
  formData.append("file", file);

  const response = await api.post("/datasets/upload", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return response.data; // { datasetId, fileName, status }
};

// Analiz tetikleme
export const analyzeDataset = async (datasetId) => {
  const response = await api.post(`/datasets/${datasetId}/analyze`);
  return response.data;
};

// Sonuçları/sorunları getirme
export const getDatasetSummary = async (datasetId) => {
  const response = await api.get(`/datasets/${datasetId}`);
  return response.data; // { id, fileName, status, totalIssues, pendingIssues, issues: [...] }
};

// Bir sorunu onayla/reddet
export const resolveIssue = async (issueId, approve) => {
  const response = await api.post(`/datasets/issues/${issueId}/resolve`, { approve });
  return response.data;
};

// Temiz dosyayı export et (indirilebilir dosya döner)
export const exportCleanedDataset = async (datasetId) => {
  const response = await api.get(`/datasets/${datasetId}/export`, {
    responseType: "blob", // dosya indirmesi için önemli, JSON değil ham byte bekliyoruz
  });
  return response.data;
};

export default api;