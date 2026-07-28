import { mkdir, writeFile } from "node:fs/promises";

const apiKey = process.env.EIA_API_KEY;
if (!apiKey) throw new Error("EIA_API_KEY is not configured.");

const params = new URLSearchParams({
  api_key: apiKey,
  frequency: "annual",
  start: String(new Date().getUTCFullYear() - 12),
  offset: "0",
  length: "5000"
});
params.append("data[0]", "price");
params.append("data[1]", "sales");
params.append("data[2]", "customers");
params.append("facets[sectorid][]", "RES");
params.append("sort[0][column]", "period");
params.append("sort[0][direction]", "desc");

const response = await fetch(`https://api.eia.gov/v2/electricity/retail-sales/data/?${params}`);
if (!response.ok) throw new Error(`EIA returned HTTP ${response.status}.`);
const payload = await response.json();
if (payload.error) throw new Error(`EIA returned an error: ${payload.error}`);

const states = {};
for (const row of payload.response?.data ?? []) {
  if (!row.stateDescription || row.stateid === "US") continue;
  const salesMillionKwh = Number(row.sales);
  const customers = Number(row.customers);
  const price = Number(row.price);
  if (![salesMillionKwh, customers, price].every(Number.isFinite) || customers <= 0) continue;
  const annual = {
    period: row.period,
    residentialPriceCents: price,
    averageMonthlyKwh: salesMillionKwh * 1_000_000 / customers / 12,
    residentialCustomers: customers
  };
  if (!states[row.stateDescription]) {
    states[row.stateDescription] = {
      ...annual,
      source: "U.S. Energy Information Administration, Electricity Retail Sales",
      history: []
    };
  }
  if (!states[row.stateDescription].history.some(x => x.period === annual.period)) {
    states[row.stateDescription].history.push(annual);
  }
}
if (Object.keys(states).length < 40) throw new Error(`Expected state coverage; received ${Object.keys(states).length} states.`);
for (const state of Object.values(states)) {
  state.history.sort((a, b) => a.period.localeCompare(b.period));
}

await mkdir("server/Data", { recursive: true });
await writeFile("server/Data/eia-state-energy.json", `${JSON.stringify({
  generatedAtUtc: new Date().toISOString(),
  methodology: "Annual residential sales divided by residential customers and 12; prices are state residential averages.",
  states
}, null, 2)}\n`);
console.log(`Published public EIA statistics for ${Object.keys(states).length} states.`);
