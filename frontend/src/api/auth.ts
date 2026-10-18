import { apiUrl } from "./config";

export type LoginRequest = {
  email: string;
  password: string;
};

export type ChangePasswordRequest = {
  currentPassword: string;
  newPassword: string;
};

export type CurrentUser = {
  id: string;
  fullName: string;
  email: string;
  role: string;
  departmentName: string | null;
};

type ApiProblem = {
  detail?: string;
  errors?: Record<string, string[]>;
};

function getProblemMessage(
  problem: ApiProblem | null,
  fallbackMessage: string,
): string {
  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat()[0]
    : undefined;

  return validationMessage ?? problem?.detail ?? fallbackMessage;
}

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

export async function changePassword(
  input: ChangePasswordRequest,
): Promise<void> {
  const response = await fetch(apiUrl("/auth/change-password"), {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(input),
  });

  if (!response.ok) {
    const problem = (await response
      .json()
      .catch(() => null)) as ApiProblem | null;

    throw new Error(
      getProblemMessage(problem, "Unable to change the password."),
    );
  }
}
