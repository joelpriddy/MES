export const CATALOG_API = import.meta.env.VITE_CATALOG_API ?? "http://localhost:5080";
export const INVENTORY_API = import.meta.env.VITE_INVENTORY_API ?? "http://localhost:5081";
export const ORDERS_API = import.meta.env.VITE_ORDERS_API ?? "http://localhost:5082";

async function get(url: string): Promise<Response> {
  const res = await fetch(url, { method: "GET" });
  return res;
}

export async function pingInventory(): Promise<"ok" | "fail"> {
  try {
    const res = await get(`${INVENTORY_API}/healthz`);
    return res.ok ? "ok" : "fail";
  } catch {
    return "fail";
  }
}

export async function pingOrders(): Promise<"ok" | "fail"> {
  try {
    const res = await fetch(`${ORDERS_API}/healthz`);
    return res.ok ? "ok" : "fail";
  } catch {
    return "fail";
  }
}

export async function pingCatalog(): Promise<"ok" | "fail"> {
  try {
    const res = await fetch(`${CATALOG_API}/healthz`);
    return res.ok ? "ok" : "fail";
  } catch {
    return "fail";
  }
}

export interface StockItemDto {
  id: number;
  productId: number;
  siteId: number;
  lotNumber: string;
  expiration: string;
  onHand: number;
  reserved: number;
  updatedOn: string;
}

export async function getStock(productId: number, siteId: number): Promise<StockItemDto[]> {
  const url = new URL(`${INVENTORY_API}/stock`);

  url.searchParams.set("productId", String(productId));
  url.searchParams.set("siteId", String(siteId));

  const res = await fetch(url.toString(), { method: "GET" });

  if (!res.ok) {
    throw new Error(`GET /stock failed: ${res.status} ${res.statusText}`);
  }

  return res.json();
}

export interface OrderLineCreateDto {
  productId: number;
  quantity: number;
  unitPrice: number;
}

export interface OrderLineDto extends OrderLineCreateDto {
  id: number;
}

export interface OrderDto {
  id: number;
  siteId: number;
  customerId: number;
  status: string;
  total: number;
  createdOn: string;
  paidOn?: string | null;
  shippedOn?: string | null;
  lines: OrderLineDto[];
}

export async function createOrder(payload: {
  siteId: number;
  customerId: number;
  lines: OrderLineCreateDto[];
}): Promise<OrderDto> {
  const res = await fetch(`${ORDERS_API}/orders`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");

    throw new Error(`POST /orders failed: ${res.status} ${res.statusText} ${text}`);
  }

  return res.json();
}

export async function shipOrder(id: number): Promise<OrderDto> {
  const res = await fetch(`${ORDERS_API}/orders/${id}/ship`, { method: "PUT" });

  if (!res.ok) {
    const text = await res.text().catch(() => "");

    throw new Error(`PUT /orders/${id}/ship failed: ${res.status} ${res.statusText} ${text}`);
  }

  return res.json();
}

export async function cancelOrder(id: number): Promise<OrderDto> {
  const res = await fetch(`${ORDERS_API}/orders/${id}/cancel`, { method: "PUT" });

  if (!res.ok) {
    const text = await res.text().catch(() => "");

    throw new Error(`PUT /orders/${id}/cancel failed: ${res.status} ${res.statusText} ${text}`);
  }

  return res.json();
}

export async function getOrder(id: number): Promise<OrderDto> {
  const res = await fetch(`${ORDERS_API}/orders/${id}`);

  if (!res.ok) {
    const text = await res.text().catch(() => "");

    throw new Error(`GET /orders/${id} failed: ${res.status} ${res.statusText} ${text}`);
  }

  return res.json();
}

export async function listOrders(skip: number = 0, take: number = 10): Promise<OrderDto[]> {
  const res = await fetch(`${ORDERS_API}/orders?skip=${skip}&take=${take}`);

  if (!res.ok) {
    const text = await res.text().catch(() => "");

    throw new Error(`GET /orders failed: ${res.status} ${res.statusText} ${text}`);
  }

  return res.json();
}

export type StockIntakeDto = {
  productId: number;
  siteId: number;
  lotNumber: string;
  expiration?: string | null;
  quantity: number;
};

export async function intakeStock(payload: StockIntakeDto): Promise<StockItemDto> {
  const res = await fetch(`${INVENTORY_API}/stock/intake`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");

    throw new Error(`POST /stock/intake failed: ${res.status} ${res.statusText} ${text}`);
  }

  return res.json();
}

export type StockConsumeDto = {
  productId: number;
  siteId: number;
  lotNumber?: string | null;
  quantity: number;
};

export async function consumeStock(payload: StockConsumeDto): Promise<StockItemDto> {
  const res = await fetch(`${INVENTORY_API}/stock/consume`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const text = await res.text().catch(() => "");

    throw new Error(`POST /stock/consume failed: ${res.status} ${res.statusText} ${text}`);
  }

  return res.json();
}

export async function listStock(params?: { productId?: number; siteId?: number; lotNumber?: string }): Promise<StockItemDto[]> {
  const q = new URLSearchParams();

  if (params?.productId != null) { q.set("productId", String(params.productId)); }
  if (params?.siteId != null) { q.set("siteId", String(params.siteId)); }
  if (params?.lotNumber) { q.set("lotNumber", params.lotNumber); }

  const url = `${INVENTORY_API}/stock${q.toString() ? `?${q.toString()}` : ""}`;
  const res = await fetch(url);

  if (!res.ok) {
    const text = await res.text().catch(() => "");

    throw new Error(`GET /stock failed: ${res.status} ${res.statusText} ${text}`);
  }

  return res.json();
}

export interface ProductDto {
  id: number;
  sku: string;
  name: string;
  description?: string;
  isManufactured: boolean;
  unitOfMeasure: string;
  subtype?: string;
  sizeLb?: number;
  priceRetail: number;
  priceWholesale?: number;
  active: boolean;
}

export async function listProducts(): Promise<ProductDto[]> {
  const res = await fetch(`${CATALOG_API}/products`);
  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`GET /products failed: ${res.status} ${res.statusText} ${text}`);
  }
  return res.json();
}
