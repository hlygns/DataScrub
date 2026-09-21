import axios from "axios";

// Backend adresi. Docker/farklı ortamlar için .env üzerinden override edilebilir
// (frontend/.env.example'a bak), verilmezse lokal geliştirme adresine düşer.
const API_BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5000/api";

const api = axios.create({
  baseURL: API_BASE_URL,
});

// Axios hatalarını kullanıcıya gösterilebilir tek bir metne indirger.
// Backend bazen düz string, bazen ProblemDetails JSON'ı dönüyor; ikisini de karşılıyoruz.
export const describeError = (err) => {
  const data = err?.response?.data;
  if (typeof data === "string" && data.trim()) return data;
  if (data?.detail) return data.detail;
  if (data?.title) return data.title;
  if (err?.code === "ERR_NETWORK") return "Sunucuya ulaşılamadı. Backend çalışıyor mu?";
  return err?.message ?? "Bilinmeyen bir hata oluştu.";
};

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

// Daha önce yüklenmiş tüm dataset'ler (geçmiş listesi için)
export const getAllDatasets = async () => {
  const response = await api.get("/datasets");
  return response.data;
};

// Bir sorunu onayla/reddet
export const resolveIssue = async (issueId, approve) => {
  const response = await api.post(`/datasets/issues/${issueId}/resolve`, { approve });
  return response.data;
};

// Birden fazla sorunu tek istekte onayla/reddet.
// Yüzlerce issue için tek tek istek atmak yerine tek round-trip - hem hızlı,
// hem de backend'de tek SaveChanges ile atomik olarak yazılıyor.
export const resolveIssuesBulk = async (issueIds, approve) => {
  const response = await api.post("/datasets/issues/resolve-bulk", { issueIds, approve });
  return response.data; // { updatedCount }
};

// Temiz dosyayı export et (indirilebilir dosya döner)
export const exportCleanedDataset = async (datasetId) => {
  const response = await api.get(`/datasets/${datasetId}/export`, {
    responseType: "blob", // dosya indirmesi için önemli, JSON değil ham byte bekliyoruz
  });

  // Dosya adını sunucunun Content-Disposition başlığından okuyoruz; böylece .xlsx
  // yüklenmişse .xlsx olarak iniyor (eskiden her şey "temizlenmis_veri.csv" oluyordu).
  const disposition = response.headers["content-disposition"] ?? "";
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  const fileName = match ? decodeURIComponent(match[1]) : "temizlenmis_veri.csv";

  return { blob: response.data, fileName };
};

export default api;
