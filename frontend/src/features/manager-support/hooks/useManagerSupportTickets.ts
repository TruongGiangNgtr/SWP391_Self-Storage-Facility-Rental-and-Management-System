import { useCallback, useEffect, useMemo, useRef, useState, type Dispatch, type SetStateAction } from "react";
import {
  assignSupportTicketStaff,
  getSupportTicket,
  listSupportTickets,
  normalizeApiError,
} from "../../../api/supportTicketApi";
import { useEmployeeLabelLookup } from "../../../components/EmployeeEntityLabel";
import { fetchAllEmployeePages } from "../../../utils/employeePresentation";
import type {
  ApiErrorShape,
  PaginationMeta,
  SupportTicketCategory,
  SupportTicketDetail,
  SupportTicketStatus,
} from "../models/supportTicket";

const PAGE_SIZE = 20;

export interface ManagerSupportFilters {
  status: "ALL" | SupportTicketStatus;
  category: "ALL" | SupportTicketCategory;
  search: string;
}

export function useManagerSupportTickets() {
  const lookupLabel = useEmployeeLabelLookup();
  const [tickets, setTickets] = useState<SupportTicketDetail[]>([]);
  const requestRef = useRef(0);
  const [requestedPage, setRequestedPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiErrorShape | null>(null);
  const [filters, setFiltersState] = useState<ManagerSupportFilters>({
    status: "ALL",
    category: "ALL",
    search: "",
  });

  const setFilters: Dispatch<SetStateAction<ManagerSupportFilters>> = useCallback((next) => {
    setRequestedPage(1);
    setFiltersState(next);
  }, []);

  const fetchAll = useCallback(async (page = 1) => {
    const targetPage = Number.isFinite(page) ? Math.max(1, Math.trunc(page)) : 1;
    setRequestedPage(targetPage);
    const requestId = ++requestRef.current;
    setLoading(true);
    setError(null);
    try {
      const result = await fetchAllEmployeePages((nextPage, pageSize) =>
        listSupportTickets({ page: nextPage, pageSize }),
      );
      if (requestId !== requestRef.current) return;
      setTickets(result.items);
    } catch (err) {
      if (requestId === requestRef.current) setError(normalizeApiError(err));
    } finally {
      if (requestId === requestRef.current) setLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchAll(1);
  }, [fetchAll]);

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
        ticket.category,
        ticket.category.replaceAll("_", " "),
        ticket.status,
        ticket.description,
        ticket.assignedEmployeeId ?? "",
        lookupLabel("contract", ticket.contractId),
        lookupLabel("contractUnit", ticket.contractId),
        lookupLabel("customer", ticket.customerId),
        lookupLabel("employee", ticket.assignedEmployeeId),
      ].some((value) => value.toLowerCase().includes(needle));
    });
  }, [filters, lookupLabel, tickets]);

  const totalPages = Math.max(1, Math.ceil(filteredTickets.length / PAGE_SIZE));
  const safePage = Math.min(requestedPage, totalPages);
  const visibleTickets = useMemo(
    () => filteredTickets.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE),
    [filteredTickets, safePage],
  );
  const pagination: PaginationMeta = useMemo(
    () => ({
      page: safePage,
      pageSize: PAGE_SIZE,
      totalItems: filteredTickets.length,
      totalPages: filteredTickets.length === 0 ? 0 : totalPages,
    }),
    [filteredTickets.length, safePage, totalPages],
  );

  const loadPage = useCallback(async (page = 1) => {
    const targetPage = Number.isFinite(page) ? Math.max(1, Math.trunc(page)) : 1;
    setRequestedPage(targetPage);
  }, []);

  const refresh = useCallback(() => fetchAll(safePage || 1), [fetchAll, safePage]);

  return {
    tickets: visibleTickets,
    rawTickets: tickets,
    pagination,
    loading,
    error,
    filters,
    setFilters,
    loadPage,
    refresh,
  };
}

export function useManagerSupportTicketDetail(ticketId: string | null) {
  const [ticket, setTicket] = useState<SupportTicketDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiErrorShape | null>(null);
  const [assigning, setAssigning] = useState(false);
  const requestRef = useRef(0);

  const refresh = useCallback(async () => {
    const requestId = ++requestRef.current;
    if (!ticketId) {
      // invalidated previous requests via ++requestRef.current
      if (requestId === requestRef.current) setLoading(false);
      if (requestId === requestRef.current) setTicket(null);
      setError(null);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await getSupportTicket(ticketId);
      if (requestId !== requestRef.current) return;
      setTicket(result.data);
    } catch (err) {
      if (requestId === requestRef.current) setError(normalizeApiError(err));
      if (requestId === requestRef.current) setTicket(null);
    } finally {
      if (requestId === requestRef.current) setLoading(false);
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
