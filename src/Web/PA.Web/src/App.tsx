import { useState } from "react";
import * as api from "./lib/api";


function App() {
  // Health
  const [invStatus, setInvStatus] = useState<"idle" | "ok" | "fail">("idle");
  const [ordStatus, setOrdStatus] = useState<"idle" | "ok" | "fail">("idle");
  const [catStatus, setCatStatus] = useState<"idle" | "ok" | "fail">("idle");
  const [pingLoading, setPingLoading] = useState(false);

  // Stock
  const [productId, setProductId] = useState<string>("2");
  const [siteId, setSiteId] = useState<string>("1");
  const [stockLoading, setStockLoading] = useState(false);
  const [stock, setStock] = useState<api.StockItemDto[] | null>(null);
  const [stockErr, setStockErr] = useState<string | null>(null);

  // Order Get
  const [getOrderId, setGetOrderId] = useState<string>("");
  const [getLoading, setGetLoading] = useState(false);
  const [getErr, setGetErr] = useState<string | null>(null);
  const [getResult, setGetResult] = useState<api.OrderDto | null>(null);

  // Order create
  const [orderSiteId, setOrderSiteId] = useState<string>("1");
  const [orderCustomerId, setOrderCustomerId] = useState<string>("1");
  const [orderProductId, setOrderProductId] = useState<string>("2");
  const [orderQty, setOrderQty] = useState<string>("1");
  const [orderUnitPrice, setOrderUnitPrice] = useState<string>("14.99");
  const [orderLoading, setOrderLoading] = useState(false);
  const [orderResult, setOrderResult] = useState<api.OrderDto | null>(null);
  const [orderErr, setOrderErr] = useState<string | null>(null);

  // Order Ship
  const [shipOrderId, setShipOrderId] = useState<string>("");
  const [shipLoading, setShipLoading] = useState(false);
  const [shipErr, setShipErr] = useState<string | null>(null);
  const [shipResult, setShipResult] = useState<api.OrderDto | null>(null);

  // Order Cancel
  const [cancelOrderId, setCancelOrderId] = useState<string>("");
  const [cancelLoading, setCancelLoading] = useState(false);
  const [cancelErr, setCancelErr] = useState<string | null>(null);
  const [cancelResult, setCancelResult] = useState<api.OrderDto | null>(null);

  // Orders List
  const [orders, setOrders] = useState<api.OrderDto[] | null>(null);
  const [ordersErr, setOrdersErr] = useState<string | null>(null);
  const [ordersLoading, setOrdersLoading] = useState(false);
  const [ordersSkip, setOrdersSkip] = useState(0);
  const [ordersTake, setOrdersTake] = useState(10);

  // Stock Intake
  const [intakePid, setIntakePid] = useState<string>("");
  const [intakeSid, setIntakeSid] = useState<string>("1");
  const [intakeLot, setIntakeLot] = useState<string>("");
  const [intakeExp, setIntakeExp] = useState<string>(""); // ISO or yyyy-mm-dd
  const [intakeQty, setIntakeQty] = useState<string>("1");
  const [intakeLoading, setIntakeLoading] = useState(false);
  const [intakeErr, setIntakeErr] = useState<string | null>(null);
  const [intakeResult, setIntakeResult] = useState<api.StockItemDto | null>(null);

  // Stock Consume
  const [consumePid, setConsumePid] = useState<string>("");
  const [consumeSid, setConsumeSid] = useState<string>("1");
  const [consumeLot, setConsumeLot] = useState<string>("");
  const [consumeQty, setConsumeQty] = useState<string>("1");
  const [consumeLoading, setConsumeLoading] = useState(false);
  const [consumeErr, setConsumeErr] = useState<string | null>(null);
  const [consumeResult, setConsumeResult] = useState<api.StockItemDto | null>(null);

  // Catalog
  const [products, setProducts] = useState<api.ProductDto[] | null>(null);
  const [prodErr, setProdErr] = useState<string | null>(null);
  const [prodLoading, setProdLoading] = useState(false);
  const [prodFilter, setProdFilter] = useState("");

  const onPing = async () => {
    setPingLoading(true);
    try {
      const [inv, ord, cat] = await Promise.all([
        api.pingInventory().catch(() => "fail" as const),
        api.pingOrders().catch(() => "fail" as const),
        api.pingCatalog().catch(() => "fail" as const),
      ]);

      setInvStatus(inv);
      setOrdStatus(ord);
      setCatStatus(cat);
    } finally {
      setPingLoading(false);
    }
  };

  const onViewStock = async () => {
    setStockErr(null);
    setStock(null);
    setStockLoading(true);
    try {
      const pid = parseInt(productId, 10);
      const sid = parseInt(siteId, 10);
      const data = await api.getStock(pid, sid);
      setStock(data);
    } catch (e: any) {
      setStockErr(e?.message ?? "Request failed");
    } finally {
      setStockLoading(false);
    }
  };

  const onGetOrder = async () => {
    setGetErr(null);
    setGetResult(null);
    setGetLoading(true);
    try {
      const id = parseInt(getOrderId, 10);
      const o = await api.getOrder(id);
      setGetResult(o);
    } catch (e: any) {
      setGetErr(e?.message ?? "Request failed");
    } finally {
      setGetLoading(false);
    }
  };

  const onCreateOrder = async () => {
    setOrderErr(null);
    setOrderResult(null);
    setOrderLoading(true);
    try {
      const payload = {
        siteId: parseInt(orderSiteId, 10),
        customerId: parseInt(orderCustomerId, 10),
        lines: [
          {
            productId: parseInt(orderProductId, 10),
            quantity: parseInt(orderQty, 10),
            unitPrice: parseFloat(orderUnitPrice),
          },
        ],
      };
      const order = await api.createOrder(payload);
      setOrderResult(order);
    } catch (e: any) {
      setOrderErr(e?.message ?? "Request failed");
    } finally {
      setOrderLoading(false);
    }
  };

  const onShipOrder = async () => {
    setShipErr(null);
    setShipResult(null);
    setShipLoading(true);
    try {
      const id = parseInt(shipOrderId, 10);
      const result = await api.shipOrder(id);
      setShipResult(result);
    } catch (e: any) {
      setShipErr(e?.message ?? "Request failed");
    } finally {
      setShipLoading(false);
    }
  };

  const onCancelOrder = async () => {
    setCancelErr(null);
    setCancelResult(null);
    setCancelLoading(true);
    try {
      const id = parseInt(cancelOrderId, 10);
      const result = await api.cancelOrder(id);
      setCancelResult(result);
    } catch (e: any) {
      setCancelErr(e?.message ?? "Request failed");
    } finally {
      setCancelLoading(false);
    }
  };

  const loadOrders = async (skip = ordersSkip, take = ordersTake) => {
    setOrdersErr(null);
    setOrdersLoading(true);
    try {
      const rows = await api.listOrders(skip, take);
      setOrders(rows);
      setOrdersSkip(skip);
      setOrdersTake(take);
    } catch (e: any) {
      setOrdersErr(e?.message ?? "Request failed");
    } finally {
      setOrdersLoading(false);
    }
  };

  const onPrev = () => {
    const nextSkip = Math.max(0, ordersSkip - ordersTake);
    if (nextSkip !== ordersSkip) { void loadOrders(nextSkip, ordersTake); }
  };

  const onNext = () => {
    const nextSkip = ordersSkip + ordersTake;
    void loadOrders(nextSkip, ordersTake);
  };

  const formatMoney = (n: number) => n.toLocaleString(undefined, { style: "currency", currency: "USD" });
  const fmtDate = (iso?: string | null) => iso ? new Date(iso).toLocaleString() : "—";

  const shipRow = async (id: number) => {
    try {
      const updated = await api.shipOrder(id);
      setOrders((prev) => prev?.map(o => o.id === id ? updated : o) ?? null);
    } catch (e: any) {
      alert(e?.message ?? "Ship failed");
    }
  };

  const cancelRow = async (id: number) => {
    try {
      const updated = await api.cancelOrder(id);
      setOrders((prev) => prev?.map(o => o.id === id ? updated : o) ?? null);
    } catch (e: any) {
      alert(e?.message ?? "Cancel failed");
    }
  };

  const onIntake = async () => {
    setIntakeErr(null);
    setIntakeResult(null);
    setIntakeLoading(true);
    try {
      const payload: api.StockIntakeDto = {
        productId: parseInt(intakePid, 10),
        siteId: parseInt(intakeSid, 10),
        lotNumber: intakeLot,
        expiration: intakeExp ? (/\d{4}-\d{2}-\d{2}/.test(intakeExp) ? `${intakeExp}T00:00:00Z` : intakeExp) : null,
        quantity: parseInt(intakeQty, 10),
      };
      const item = await api.intakeStock(payload);
      setIntakeResult(item);
    } catch (e: any) {
      setIntakeErr(e?.message ?? "Request failed");
    } finally {
      setIntakeLoading(false);
    }
  };

  const onConsume = async () => {
    setConsumeErr(null);
    setConsumeResult(null);
    setConsumeLoading(true);
    try {
      const payload: api.StockConsumeDto = {
        productId: parseInt(consumePid, 10),
        siteId: parseInt(consumeSid, 10),
        lotNumber: consumeLot || null,
        quantity: parseInt(consumeQty, 10),
      };
      const item = await api.consumeStock(payload);
      setConsumeResult(item);
    } catch (e: any) {
      setConsumeErr(e?.message ?? "Request failed");
    } finally {
      setConsumeLoading(false);
    }
  };

  const loadStock = async () => {
    setStockErr(null);
    setStockLoading(true);
    try {
      const params: { productId?: number; siteId?: number } = {};
      if (productId) { params.productId = parseInt(productId, 10); }
      if (siteId) { params.siteId = parseInt(siteId, 10); }
      const rows = await api.listStock(params);
      setStock(rows);
    } catch (e: any) {
      setStockErr(e?.message ?? "Request failed");
    } finally {
      setStockLoading(false);
    }
  };

  const loadProducts = async () => {
    setProdErr(null);
    setProdLoading(true);
    try {
      const rows = await api.listProducts();
      setProducts(rows);
    } catch (e: any) {
      setProdErr(e?.message ?? "Request failed");
    } finally {
      setProdLoading(false);
    }
  };

  return (
    <div style={{ padding: 24, fontFamily: "system-ui, sans-serif", maxWidth: 1000, margin: "0 auto" }}>
      <h1>PA Web</h1>

      {/* Health */}
      <section style={{ marginBottom: 24 }}>
        <h2>Health</h2>
        <p>Ping Inventory and Orders APIs.</p>
        <button onClick={onPing} disabled={pingLoading} style={{ padding: "8px 12px" }}>
          {pingLoading ? "Pinging..." : "Ping APIs"}
        </button>

        <div style={{ display: "flex", gap: 12, marginTop: 12, flexWrap: "wrap" }}>
          <StatusBadge label="Inventory API" status={invStatus} />
          <StatusBadge label="Orders API" status={ordStatus} />
          <StatusBadge label="Catalog API" status={catStatus} />
        </div>
      </section>

      {/* Stock */}
      <section style={{ marginBottom: 24 }}>
        <h2>View Stock</h2>
        <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
          <label>
            Product Id:&nbsp;
            <input type="number" value={productId} onChange={(e) => setProductId(e.target.value)} style={{ width: 120 }} />
          </label>
          <label>
            Site Id:&nbsp;
            <input type="number" value={siteId} onChange={(e) => setSiteId(e.target.value)} style={{ width: 120 }} />
          </label>
          <button onClick={onViewStock} disabled={stockLoading} style={{ padding: "8px 12px" }}>
            {stockLoading ? "Loading..." : "Get Stock"}
          </button>
        </div>

        {stockErr && <div style={{ color: "crimson", marginTop: 8 }}>Error: {stockErr}</div>}

        {stock && stock.length === 0 && <div style={{ marginTop: 8 }}>No stock found.</div>}

        {stock && stock.length > 0 && (
          <table style={{ width: "100%", borderCollapse: "collapse", border: "1px solid #ddd", marginTop: 8, fontSize: 14 }}>
            <thead>
              <tr>
                <th style={th}>Id</th>
                <th style={th}>Product</th>
                <th style={th}>Site</th>
                <th style={th}>Lot</th>
                <th style={th}>Expiration</th>
                <th style={th}>On Hand</th>
                <th style={th}>Reserved</th>
                <th style={th}>Updated</th>
              </tr>
            </thead>
            <tbody>
              {stock.map((s) => (
                <tr key={s.id}>
                  <td style={td}>{s.id}</td>
                  <td style={td}>{s.productId}</td>
                  <td style={td}>{s.siteId}</td>
                  <td style={td}>{s.lotNumber}</td>
                  <td style={td}>{new Date(s.expiration).toLocaleDateString()}</td>
                  <td style={td}>{s.onHand}</td>
                  <td style={td}>{s.reserved}</td>
                  <td style={td}>{new Date(s.updatedOn).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {/* Create Order */}
      <section>
        <h2>Create Order</h2>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(3, minmax(160px, 1fr))", gap: 12, marginBottom: 12 }}>
          <label>
            Site Id
            <input type="number" value={orderSiteId} onChange={(e) => setOrderSiteId(e.target.value)} style={input} />
          </label>
          <label>
            Customer Id
            <input type="number" value={orderCustomerId} onChange={(e) => setOrderCustomerId(e.target.value)} style={input} />
          </label>
          <label>
            Product Id
            <input type="number" value={orderProductId} onChange={(e) => setOrderProductId(e.target.value)} style={input} />
          </label>
          <label>
            Quantity
            <input type="number" value={orderQty} onChange={(e) => setOrderQty(e.target.value)} style={input} />
          </label>
          <label>
            Unit Price
            <input type="number" step="0.01" value={orderUnitPrice} onChange={(e) => setOrderUnitPrice(e.target.value)} style={input} />
          </label>
        </div>
        <button onClick={onCreateOrder} disabled={orderLoading} style={{ padding: "8px 12px" }}>
          {orderLoading ? "Creating..." : "Create Order"}
        </button>

        {orderErr && <div style={{ color: "crimson", marginTop: 8 }}>Error: {orderErr}</div>}

        {orderResult && (
          <pre
            style={{
              marginTop: 12,
              background: "#f6f8fa",
              color: "#111",
              padding: 12,
              border: "1px solid #e5e7eb",
              borderRadius: 8,
              overflowX: "auto",
              fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
              fontSize: 13,
              lineHeight: 1.5
            }}
          >
            {JSON.stringify(orderResult, null, 2)}
          </pre>
        )}
      </section>

      {/* Ship Order */}
      <section style={{ marginTop: 24 }}>
        <h2>Ship Order</h2>
        <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
          <label>
            Order Id:&nbsp;
            <input
              type="number"
              value={shipOrderId}
              onChange={(e) => setShipOrderId(e.target.value)}
              style={{ width: 160, padding: "6px 8px" }}
            />
          </label>
          <button
            onClick={() => { if (orderResult) { setShipOrderId(String(orderResult.id)); } }}
            disabled={!orderResult}
            style={{ padding: "8px 12px" }}
          >
            Use last created ({orderResult?.id ?? "—"})
          </button>
          <button onClick={onShipOrder} disabled={shipLoading || !shipOrderId} style={{ padding: "8px 12px" }}>
            {shipLoading ? "Shipping..." : "Ship Order"}
          </button>
        </div>

        {shipErr && <div style={{ color: "crimson", marginTop: 8 }}>Error: {shipErr}</div>}

        {shipResult && (
          <pre
            style={{
              marginTop: 12,
              background: "#f6f8fa",
              color: "#111",
              padding: 12,
              border: "1px solid #e5e7eb",
              borderRadius: 8,
              overflowX: "auto",
              fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
              fontSize: 13,
              lineHeight: 1.5
            }}
          >
            {JSON.stringify(shipResult, null, 2)}
          </pre>
        )}
      </section>

      {/* Cancel Order */}
      <section style={{ marginTop: 24 }}>
        <h2>Cancel Order</h2>
        <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
          <label>
            Order Id:&nbsp;
            <input
              type="number"
              value={cancelOrderId}
              onChange={(e) => setCancelOrderId(e.target.value)}
              style={{ width: 160, padding: "6px 8px" }}
            />
          </label>
          <button
            onClick={() => { if (orderResult) { setCancelOrderId(String(orderResult.id)); } }}
            disabled={!orderResult}
            style={{ padding: "8px 12px" }}
          >
            Use last created ({orderResult?.id ?? "—"})
          </button>
          <button
            onClick={onCancelOrder}
            disabled={cancelLoading || !cancelOrderId}
            style={{ padding: "8px 12px" }}
          >
            {cancelLoading ? "Canceling..." : "Cancel Order"}
          </button>
        </div>

        {cancelErr && <div style={{ color: "crimson", marginTop: 8 }}>Error: {cancelErr}</div>}

        {cancelResult && (
          <pre
            style={{
              marginTop: 12,
              background: "#f6f8fa",
              color: "#111",
              padding: 12,
              border: "1px solid #e5e7eb",
              borderRadius: 8,
              overflowX: "auto",
              fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
              fontSize: 13,
              lineHeight: 1.5
            }}
          >
            {JSON.stringify(cancelResult, null, 2)}
          </pre>
        )}
      </section>

      {/* Get Orders */}
      <section style={{ marginTop: 24 }}>
        <h2>Get Order by Id</h2>
        <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
          <label>
            Order Id:&nbsp;
            <input
              type="number"
              value={getOrderId}
              onChange={(e) => setGetOrderId(e.target.value)}
              style={{ width: 160, padding: "6px 8px" }}
            />
          </label>
          <button
            onClick={() => { if (orderResult) { setGetOrderId(String(orderResult.id)); } }}
            disabled={!orderResult}
            style={{ padding: "8px 12px" }}
          >
            Use last created ({orderResult?.id ?? "—"})
          </button>
          <button onClick={onGetOrder} disabled={getLoading || !getOrderId} style={{ padding: "8px 12px" }}>
            {getLoading ? "Loading..." : "Get Order"}
          </button>
        </div>

        {getErr && <div style={{ color: "crimson", marginTop: 8 }}>Error: {getErr}</div>}

        {getResult && (
          <pre
            style={{
              marginTop: 12,
              background: "#f6f8fa",
              color: "#111",
              padding: 12,
              border: "1px solid #e5e7eb",
              borderRadius: 8,
              overflowX: "auto",
              fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
              fontSize: 13,
              lineHeight: 1.5
            }}
          >
            {JSON.stringify(getResult, null, 2)}
          </pre>
        )}
      </section>

      {/* Stock Intake */}
      <section style={{ marginTop: 24 }}>
        <h2>Scan Intake</h2>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(5, minmax(140px, 1fr))", gap: 12, alignItems: "end", marginBottom: 12 }}>
          <label>
            Product Id
            <input
              type="number"
              value={intakePid}
              onChange={(e) => setIntakePid(e.target.value)}
              placeholder="e.g. 2"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
          <label>
            Site Id
            <input
              type="number"
              value={intakeSid}
              onChange={(e) => setIntakeSid(e.target.value)}
              placeholder="1"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
          <label>
            Lot #
            <input
              value={intakeLot}
              onChange={(e) => setIntakeLot(e.target.value)}
              placeholder="Scan / type lot"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
          <label>
            Expiration
            <input
              value={intakeExp}
              onChange={(e) => setIntakeExp(e.target.value)}
              placeholder="YYYY-MM-DD or ISO"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
          <label>
            Quantity
            <input
              type="number"
              value={intakeQty}
              onChange={(e) => setIntakeQty(e.target.value)}
              placeholder="1"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
        </div>

        <div style={{ display: "flex", gap: 12 }}>
          <button onClick={onIntake} disabled={intakeLoading || !intakePid || !intakeLot || !intakeQty} style={{ padding: "8px 12px" }}>
            {intakeLoading ? "Submitting..." : "Add to Inventory"}
          </button>
          <button
            onClick={() => {
              setIntakePid("");
              setIntakeLot("");
              setIntakeExp("");
              setIntakeQty("1");
            }}
            disabled={intakeLoading}
            style={{ padding: "8px 12px" }}
          >
            Clear
          </button>
        </div>

        {intakeErr && <div style={{ color: "crimson", marginTop: 8 }}>Error: {intakeErr}</div>}

        {intakeResult && (
          <pre
            style={{
              marginTop: 12,
              background: "#f6f8fa",
              color: "#111",
              padding: 12,
              border: "1px solid #e5e7eb",
              borderRadius: 8,
              overflowX: "auto",
              fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
              fontSize: 13,
              lineHeight: 1.5
            }}
          >
            {JSON.stringify(intakeResult, null, 2)}
          </pre>
        )}
      </section>

      {/* Stock Consume */}
      <section style={{ marginTop: 24 }}>
        <h2>Scan Use</h2>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(4, minmax(140px, 1fr))", gap: 12, alignItems: "end", marginBottom: 12 }}>
          <label>
            Product Id
            <input
              type="number"
              value={consumePid}
              onChange={(e) => setConsumePid(e.target.value)}
              placeholder="e.g. 2"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
          <label>
            Site Id
            <input
              type="number"
              value={consumeSid}
              onChange={(e) => setConsumeSid(e.target.value)}
              placeholder="1"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
          <label>
            Lot # (optional)
            <input
              value={consumeLot}
              onChange={(e) => setConsumeLot(e.target.value)}
              placeholder="Scan / type lot"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
          <label>
            Quantity
            <input
              type="number"
              value={consumeQty}
              onChange={(e) => setConsumeQty(e.target.value)}
              placeholder="1"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
        </div>

        <div style={{ display: "flex", gap: 12 }}>
          <button
            onClick={onConsume}
            disabled={consumeLoading || !consumePid || !consumeQty}
            style={{ padding: "8px 12px" }}
          >
            {consumeLoading ? "Submitting..." : "Consume"}
          </button>
          <button
            onClick={() => { setConsumePid(""); setConsumeLot(""); setConsumeQty("1"); }}
            disabled={consumeLoading}
            style={{ padding: "8px 12px" }}
          >
            Clear
          </button>
        </div>

        {consumeErr && <div style={{ color: "crimson", marginTop: 8 }}>Error: {consumeErr}</div>}

        {consumeResult && (
          <pre
            style={{
              marginTop: 12,
              background: "#f6f8fa",
              color: "#111",
              padding: 12,
              border: "1px solid #e5e7eb",
              borderRadius: 8,
              overflowX: "auto",
              fontFamily: "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
              fontSize: 13,
              lineHeight: 1.5
            }}
          >
            {JSON.stringify(consumeResult, null, 2)}
          </pre>
        )}
      </section>

      {/* Stock List */}
      <section style={{ marginTop: 24 }}>
        <h2>Inventory</h2>

        <div style={{ display: "grid", gridTemplateColumns: "repeat(3, minmax(140px, 1fr))", gap: 12, alignItems: "end", marginBottom: 12 }}>
          <label>
            Product Id
            <input
              type="number"
              value={productId}
              onChange={(e) => setProductId(e.target.value)}
              placeholder="e.g. 2"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
          <label>
            Site Id
            <input
              type="number"
              value={siteId}
              onChange={(e) => setSiteId(e.target.value)}
              placeholder="1"
              style={{ width: "100%", padding: "6px 8px" }}
            />
          </label>
          <div style={{ display: "flex", gap: 8 }}>
            <button onClick={loadStock} disabled={stockLoading} style={{ padding: "8px 12px" }}>
              {stockLoading ? "Loading..." : "Load"}
            </button>
            <button
              onClick={() => { setProductId(""); setSiteId("1"); setStock(null); setStockErr(null); }}
              disabled={stockLoading}
              style={{ padding: "8px 12px" }}
            >
              Clear
            </button>
          </div>
        </div>

        {stockErr && <div style={{ color: "crimson", marginBottom: 8 }}>Error: {stockErr}</div>}

        {stock && stock.length > 0 && (
          <div style={{ overflowX: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead>
                <tr>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Id</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Product</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Site</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Lot</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Expiration</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>On Hand</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Reserved</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Updated</th>
                </tr>
              </thead>
              <tbody>
                {stock.map(s => (
                  <tr key={s.id}>
                    <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{s.id}</td>
                    <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{s.productId}</td>
                    <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{s.siteId}</td>
                    <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{s.lotNumber ?? "—"}</td>
                    <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>
                      {s.expiration ? new Date(s.expiration as any).toLocaleDateString() : "—"}
                    </td>
                    <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{s.onHand}</td>
                    <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{s.reserved}</td>
                    <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>
                      {s.updatedOn ? new Date(s.updatedOn as any).toLocaleString() : "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {stock && stock.length === 0 && <div style={{ color: "#6b7280" }}>No stock found.</div>}
      </section>

      {/* List Orders */}
      <section style={{ marginTop: 24 }}>
        <h2>Orders</h2>

        <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
          <label>
            Page size:&nbsp;
            <input
              type="number"
              value={ordersTake}
              onChange={(e) => setOrdersTake(Math.max(1, parseInt(e.target.value || "10", 10)))}
              style={{ width: 100, padding: "6px 8px" }}
            />
          </label>
          <button onClick={() => loadOrders(0, ordersTake)} disabled={ordersLoading} style={{ padding: "8px 12px" }}>
            {ordersLoading ? "Loading..." : "Load Orders"}
          </button>
          <button onClick={onPrev} disabled={ordersLoading || ordersSkip === 0} style={{ padding: "8px 12px" }}>Prev</button>
          <button onClick={onNext} disabled={ordersLoading} style={{ padding: "8px 12px" }}>Next</button>
          <span style={{ color: "#6b7280" }}>Offset: {ordersSkip}</span>
        </div>

        {ordersErr && <div style={{ color: "crimson", marginBottom: 12 }}>Error: {ordersErr}</div>}

        {orders && orders.length > 0 && (
          <div style={{ overflowX: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead>
                <tr>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Id</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Status</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Total</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Created</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Paid</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Shipped</th>
                  <th style={{ textAlign: "left", borderBottom: "1px solid #e5e7eb", padding: "8px" }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {orders.map(o => {
                  const statusColor =
                    o.status === "Shipped" ? "#16a34a" :
                      o.status === "Canceled" ? "#dc2626" :
                        "#111";
                  return (
                    <tr key={o.id}>
                      <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{o.id}</td>
                      <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6", color: statusColor }}>{o.status}</td>
                      <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{formatMoney(o.total)}</td>
                      <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{fmtDate(o.createdOn as any)}</td>
                      <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{fmtDate(o.paidOn as any)}</td>
                      <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>{fmtDate(o.shippedOn as any)}</td>
                      <td style={{ padding: "8px", borderBottom: "1px solid #f3f4f6" }}>
                        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                          <button onClick={() => shipRow(o.id)} disabled={o.status !== "Pending"} style={{ padding: "6px 10px" }}>
                            Ship
                          </button>
                          <button onClick={() => cancelRow(o.id)} disabled={o.status !== "Pending"} style={{ padding: "6px 10px" }}>
                            Cancel
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {orders && orders.length === 0 && (
          <div style={{ marginTop: 8, color: "#6b7280" }}>No orders found.</div>
        )}
      </section>

      { /* Products */}
      <section style={{ marginTop: 24 }}>
        <h2>Product Lookup</h2>

        <div style={{ display: "flex", gap: 8, alignItems: "center", marginBottom: 12 }}>
          <input
            value={prodFilter}
            onChange={(e) => setProdFilter(e.target.value)}
            placeholder="Search by name or SKU..."
            style={{ flex: 1, padding: "8px 10px" }}
          />
          <button onClick={loadProducts} disabled={prodLoading} style={{ padding: "8px 12px" }}>
            {prodLoading ? "Loading..." : "Refresh"}
          </button>
        </div>

        {prodErr && <div style={{ color: "crimson", marginBottom: 8 }}>Error: {prodErr}</div>}

        {products && (
          <div style={{ maxHeight: 260, overflowY: "auto", border: "1px solid #e5e7eb", borderRadius: 8 }}>
            {products
              .filter(p => {
                if (!prodFilter.trim()) { return true; }
                const q = prodFilter.toLowerCase();
                return p.name.toLowerCase().includes(q) || p.sku.toLowerCase().includes(q);
              })
              .map(p => (
                <button
                  key={p.id}
                  onClick={() => setProductId(String(p.id))}
                  style={{
                    width: "100%",
                    textAlign: "left",
                    padding: "8px 12px",
                    borderBottom: "1px solid #f3f4f6",
                    cursor: "pointer",
                    background: "white"
                  }}
                  title="Use this product ID for Intake/Use/Orders"
                >
                  <div style={{ fontWeight: 600 }}>{p.name}</div>
                  <div style={{ fontSize: 12, color: "#6b7280" }}>
                    SKU: {p.sku} • ID: {p.id} • {p.isManufactured ? "Manufactured" : "Resale"}
                    {p.sizeLb ? ` • ${p.sizeLb}${p.unitOfMeasure}` : ""}
                    {p.flavor ? ` • ${p.flavor}` : ""}
                  </div>
                </button>
              ))
            }
            {products.length === 0 && (
              <div style={{ padding: 12, color: "#6b7280" }}>No products.</div>
            )}
          </div>
        )}

        {!products && !prodLoading && (
          <div style={{ color: "#6b7280" }}>No products loaded yet.</div>
        )}

        <div style={{ marginTop: 8, color: "#6b7280" }}>
          Selected Product Id: <span style={{ fontWeight: 600 }}>{productId || "—"}</span>
        </div>
      </section>

    </div>
  );
}

function StatusBadge({ label, status }: { label: string; status: "idle" | "ok" | "fail" }) {
  const color = status === "ok" ? "#16a34a" : status === "fail" ? "#dc2626" : "#6b7280";
  const icon = status === "ok" ? "✅" : status === "fail" ? "❌" : "—";

  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 8, color }}>
      <span>{icon}</span>
      <span>{label}: {status === "idle" ? "—" : status.toUpperCase()}</span>
    </span>
  );
}

const th: React.CSSProperties = { textAlign: "left", borderBottom: "1px solid #eee", padding: "8px" };
const td: React.CSSProperties = { borderBottom: "1px solid #f4f4f4", padding: "8px" };
const input: React.CSSProperties = { display: "block", width: "100%", padding: "6px 8px", marginTop: 6 };

export default App;
