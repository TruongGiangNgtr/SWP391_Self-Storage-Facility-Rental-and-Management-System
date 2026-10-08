import { useCallback, useEffect, useMemo, useState } from "react";
import {
  assignSupportTicketStaff,
  getSupportTicket,
  listSupportTickets,
  normalizeApiError,
} from "../../../../api/supportTicketApi";
import type {
  ApiErrorShape,
  PaginationMeta,
  SupportTicketCategory,
  SupportTicketDetail,
  SupportTicketStatus,
} from "../models/supportTicket";

const DEFAULT_PAGINATION: PaginationMeta = {
  page: 1,
  pageSize: 20,
  totalItems: 0,
  totalPages: 0,
};

export interface ManagerSupportFilters {
  status: "ALL" | SupportTicketStatus;
  category: "ALL" | SupportTicketCategory;
  search: string;
}

export function useManagerSupportTickets() {
  const [tickets, setTickets] = useState<SupportTicketDetail[]>([]);
  const [pagination, setPagination] = useState(DEFAULT_PAGINATION);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiErrorShape | null>(null);
  const [filters, setFilters] = useState<ManagerSupportFilters>({
    status: "ALL",
    category: "ALL",
    search: "",
  });

  const load = useCallback(async (page = 1) => {
    setLoading(true);
    setError(null);
    try {
      const result = await listSupportTickets({ page, pageSize: 20 });
      setTickets(result.data ?? []);
      setPagination(result.pagination ?? { ...DEFAULT_PAGINATION, page });
    } catch (err) {
      setError(normalizeApiError(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load(1);
  }, [load]);

  const filteredTickets = useMemo(() => {
    const needle = filters.search.trim().toLowerCase();
    return tickets.filter((ticket) => {
      if (filters.status !== "ALL" && ticket.status !== filters.status) return false;
      if (filters.category !== "ALL" && ticket.category !== filters.category) return false;
      if (!needle) return true;

      return [
        ticket.supportTicketId,
        ticket.contractId,
        ticket.customerId,
        ticket.description,
        ticket.assignedEmployeeId ?? "",
      ].some((value) => value.toLowerCase().includes(needle));
    });
  }, [filters, tickets]);

  const refresh = useCallback(() => load(pagination.page || 1), [load, pagination.page]);

  return {
    tickets: filteredTickets,
    rawTickets: tickets,
    pagination,
    loading,
    error,
    filters,
    setFilters,
    loadPage: load,
    refresh,
  };
}

export function useManagerSupportTicketDetail(ticketId: string | null) {
  const [ticket, setTicket] = useState<SupportTicketDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiErrorShape | null>(null);
  const [assigning, setAssigning] = useState(false);

  const refresh = useCallback(async () => {
    if (!ticketId) {
      setTicket(null);
      setError(null);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await getSupportTicket(ticketId);
      setTicket(result.data);
    } catch (err) {
      setError(normalizeApiError(err));
      setTicket(null);
    } finally {
      setLoading(false);
    }
  }, [ticketId]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const assign = useCallback(
    async (employeeId: string) => {
      if (!ticketId) return { ok: false as const, error: null };
      setAssigning(true);
      setError(null);
      try {
        await assignSupportTicketStaff(ticketId, { employeeId });
        await refresh();
        return { ok: true as const, error: null };
      } catch (err) {
        const apiError = normalizeApiError(err);
        setError(apiError);
        // SRS FE rule: a 409 may mean displayed lifecycle state is stale.
        if (apiError.code === "SUPPORT_TICKET_INVALID_STATUS") {
          await refresh();
        }
        return { ok: false as const, error: apiError };
      } finally {
        setAssigning(false);
      }
    },
    [refresh, ticketId],
  );

  return { ticket, loading, error, assigning, refresh, assign };
}
