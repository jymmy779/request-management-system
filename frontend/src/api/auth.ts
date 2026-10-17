import { apiUrl } from "./config";

export type LoginRequest = {
  email: string;
  password: string;
};

export type CurrentUser = {
  id: string;
  email: string;
  role: string;
  departmentName: string | null;
};

type ApiProblem = {
  detail?: string;
};

export async function login(loginRequest: LoginRequest): Promise<void> {
  const response = await fetch(apiUrl("/auth/login"), {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(loginRequest),
  });

  if (!response.ok) {
    const problem = (await response
      .json()
      .catch(() => null)) as ApiProblem | null;

    throw new Error(problem?.detail ?? "Unable to sign in.");
  }
}

export async function logout(): Promise<void> {
  const response = await fetch(apiUrl("/auth/logout"), {
    method: "POST",
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error("Unable to sign out.");
  }
}

export async function getCurrentUser(): Promise<CurrentUser | null> {
  const response = await fetch(apiUrl("/auth/me"), {
    credentials: "include",
  });

  if (response.status === 401) {
    return null;
  }

  if (!response.ok) {
    throw new Error("Unable to load the current user.");
  }

  return (await response.json()) as CurrentUser;
}
