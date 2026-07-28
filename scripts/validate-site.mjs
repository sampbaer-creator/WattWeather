import { access, readFile } from "node:fs/promises";

const pages = [
  "index.html",
  "solar.html",
  "incentives.html",
  "weather-impact.html",
  "power.html",
  "energy.html"
];

const failures = [];

for (const page of pages) {
  const html = await readFile(page, "utf8");
  const ids = [...html.matchAll(/\sid="([^"]+)"/g)].map(match => match[1]);
  const duplicates = ids.filter((id, index) => ids.indexOf(id) !== index);
  if (duplicates.length) failures.push(`${page}: duplicate IDs: ${[...new Set(duplicates)].join(", ")}`);

  for (const match of html.matchAll(/(?:href|src)="([^"]+)"/g)) {
    const reference = match[1];
    if (/^(?:https?:|data:|#|mailto:)/.test(reference)) continue;
    const localPath = reference.split(/[?#]/, 1)[0];
    if (!localPath) continue;
    try {
      await access(localPath);
    } catch {
      failures.push(`${page}: missing ${reference}`);
    }
  }
}

const energyData = JSON.parse(await readFile("data/eia-state-energy.json", "utf8"));
const stateCount = Object.keys(energyData.states ?? {}).length;
if (stateCount < 50) failures.push(`EIA dataset contains only ${stateCount} state entries`);

if (failures.length) {
  console.error(failures.join("\n"));
  process.exitCode = 1;
} else {
  console.log(`Validated ${pages.length} pages and ${stateCount} EIA state entries.`);
}
