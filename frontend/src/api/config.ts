const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL;

if (!configuredApiBaseUrl) {
  throw new Error("VITE_API_BASE_URL is not configured.");
}

const apiBaseUrl = configuredApiBaseUrl.replace(/\/$/, "");
const apiBasePath = "/api";

export function apiUrl(path: string): string {
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;

  return `${apiBaseUrl}${apiBasePath}${normalizedPath}`;
}
