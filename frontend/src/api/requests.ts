import { apiUrl } from "./config";

export type RequestStatus =
  | "Pending"
  | "Approved"
  | "Rejected"
  | "Completed"
  | "Cancelled";

export type RequestCategory = "Equipment" | "Software" | "Other";

export type RequestPriority = "Low" | "Medium" | "High";

export type RequestItem = {
  id: number;
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
  quantity: number;
  estimatedCost: number | null;
  status: RequestStatus;
  requesterId: string;
  requesterFullName: string;
  requesterEmail: string;
  requesterDepartmentName: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type RequestQuery = {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: RequestStatus;
  category?: RequestCategory;
  sortBy?: "createdAt" | "title" | "status";
  sortDirection?: "asc" | "desc";
};

export type CreateRequestInput = {
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
  quantity: number;
  estimatedCost: number | null;
};

type ApiValidationProblem = {
  detail?: string;
  title?: string;
  errors?: Record<string, string[]>;
};

export async function getRequests(
  query: RequestQuery = {},
): Promise<PagedResult<RequestItem>> {
  const searchParameters = new URLSearchParams();

  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== "") {
      searchParameters.set(key, String(value));
    }
  }

  const queryString = searchParameters.toString();
  const path = queryString ? `/requests?${queryString}` : "/requests";

  const response = await fetch(apiUrl(path), {
    credentials: "include",
  });

  if (response.status === 401) {
    throw new Error("Your session has expired. Please sign in again.");
  }

  if (!response.ok) {
    throw new Error("Unable to load requests.");
  }

  return (await response.json()) as PagedResult<RequestItem>;
}

export async function getRequestById(id: number): Promise<RequestItem> {
  const response = await fetch(apiUrl(`/requests/${id}`), {
    credentials: "include",
  });

  if (response.status === 401) {
    throw new Error("Your session has expired. Please sign in again.");
  }

  if (response.status === 404) {
    throw new Error("Request not found or you do not have access to it.");
  }

  if (!response.ok) {
    throw new Error("Unable to load the request.");
  }

  return (await response.json()) as RequestItem;
}

export async function createRequest(
  input: CreateRequestInput,
): Promise<RequestItem> {
  const response = await fetch(apiUrl("/requests"), {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(input),
  });

  if (response.status === 401) {
    throw new Error("Your session has expired. Please sign in again.");
  }

  if (!response.ok) {
    const problem = (await response
      .json()
      .catch(() => null)) as ApiValidationProblem | null;
    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat()[0]
      : undefined;

    throw new Error(
      validationMessage ??
        problem?.detail ??
        problem?.title ??
        "Unable to create the request.",
    );
  }

  return (await response.json()) as RequestItem;
}

export async function updateRequest(
  id: number,
  input: CreateRequestInput,
): Promise<RequestItem> {
  const response = await fetch(apiUrl(`/requests/${id}`), {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(input),
  });

  if (response.status === 401) {
    throw new Error("Your session has expired. Please sign in again.");
  }

  if (response.status === 403) {
    throw new Error("Only the request owner can edit this request.");
  }

  if (response.status === 404) {
    throw new Error("Request not found.");
  }

  if (response.status === 409) {
    throw new Error("Only a Pending request can be edited.");
  }

  if (!response.ok) {
    const problem = (await response
      .json()
      .catch(() => null)) as ApiValidationProblem | null;
    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat()[0]
      : undefined;

    throw new Error(
      validationMessage ??
        problem?.detail ??
        problem?.title ??
        "Unable to update the request.",
    );
  }

  return (await response.json()) as RequestItem;
}

export type RequestHistoryItem = {
  id: number;
  requestId: number;
  fromStatus: RequestStatus | null;
  toStatus: RequestStatus;
  changedByUserId: string;
  changedByFullName: string;
  changedByEmail: string;
  note: string | null;
  changedAt: string;
};

export async function getRequestHistory(
  id: number,
): Promise<RequestHistoryItem[]> {
  const response = await fetch(apiUrl(`/requests/${id}/history`), {
    credentials: "include",
  });

  if (response.status === 401) {
    throw new Error("Your session has expired. Please sign in again.");
  }

  if (response.status === 404) {
    throw new Error("Request history not found or you do not have access to it.");
  }

  if (!response.ok) {
    throw new Error("Unable to load request history.");
  }

  return (await response.json()) as RequestHistoryItem[];
}

export async function approveRequest(id: number): Promise<RequestItem> {
  const response = await fetch(apiUrl(`/requests/${id}/approve`), {
    method: "POST",
    credentials: "include",
  });

  if (response.status === 401) {
    throw new Error("Your session has expired. Please sign in again.");
  }

  if (response.status === 403) {
    throw new Error("You are not allowed to approve this request (e.g. self-approval is forbidden).");
  }

  if (response.status === 404) {
    throw new Error("Request not found.");
  }

  if (response.status === 409) {
    throw new Error("Only a Pending request can be approved, or it was modified by another operation.");
  }

  if (!response.ok) {
    throw new Error("Unable to approve the request.");
  }

  return (await response.json()) as RequestItem;
}

export async function rejectRequest(
  id: number,
  reason: string,
): Promise<RequestItem> {
  const response = await fetch(apiUrl(`/requests/${id}/reject`), {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ reason }),
  });

  if (response.status === 401) {
    throw new Error("Your session has expired. Please sign in again.");
  }

  if (response.status === 403) {
    throw new Error("You are not allowed to reject this request.");
  }

  if (response.status === 404) {
    throw new Error("Request not found.");
  }

  if (response.status === 409) {
    throw new Error("Only a Pending request can be rejected, or it was modified by another operation.");
  }

  if (!response.ok) {
    const problem = (await response
      .json()
      .catch(() => null)) as ApiValidationProblem | null;
    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat()[0]
      : undefined;

    throw new Error(
      validationMessage ??
        problem?.detail ??
        problem?.title ??
        "Unable to reject the request.",
    );
  }

  return (await response.json()) as RequestItem;
}

export async function completeRequest(id: number): Promise<RequestItem> {
  const response = await fetch(apiUrl(`/requests/${id}/complete`), {
    method: "POST",
    credentials: "include",
  });

  if (response.status === 401) {
    throw new Error("Your session has expired. Please sign in again.");
  }

  if (response.status === 404) {
    throw new Error("Request not found.");
  }

  if (response.status === 409) {
    throw new Error("Only an Approved request can be completed.");
  }

  if (!response.ok) {
    throw new Error("Unable to complete the request.");
  }

  return (await response.json()) as RequestItem;
}

export async function cancelRequest(id: number): Promise<RequestItem> {
  const response = await fetch(apiUrl(`/requests/${id}/cancel`), {
    method: "POST",
    credentials: "include",
  });

  if (response.status === 401) {
    throw new Error("Your session has expired. Please sign in again.");
  }

  if (response.status === 403) {
    throw new Error("Only the owner can cancel this request.");
  }

  if (response.status === 404) {
    throw new Error("Request not found.");
  }

  if (response.status === 409) {
    throw new Error("Only a Pending request can be cancelled.");
  }

  if (!response.ok) {
    throw new Error("Unable to cancel the request.");
  }

  return (await response.json()) as RequestItem;
}

