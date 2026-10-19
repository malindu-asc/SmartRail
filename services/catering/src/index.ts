import express from "express";
import { menus } from "./modules/menus/menus.ts";

// RailBite is the external catering platform. It shares no code or database with SmartRail
// and is reached only over HTTP (API key), so it can be down independently.
const app = express();
app.use(express.json());

const apiKey = process.env.CATERING_API_KEY ?? "dev-catering-key-change-me";

app.get("/health", (_req, res) => {
  res.json({ status: "ok", service: "railbite-catering" });
});

// SmartRail's API calls this with its key; a passenger's phone never does.
app.get("/v1/stations/:code/menu", (req, res) => {
  if (req.header("x-api-key") !== apiKey) {
    res.status(401).json({ error: "unauthorised" });
    return;
  }

  const menu = menus[req.params.code.toUpperCase()];
  if (!menu) {
    // This station has no kitchen. It is not an error: SmartRail shows no food row.
    res.status(404).json({ error: "no-menu" });
    return;
  }

  res.json(menu);
});

export { app };

if (process.env.NODE_ENV !== "test") {
  const port = Number(process.env.PORT ?? 4000);
  app.listen(port, () => {
    console.log(`RailBite catering listening on ${port}`);
  });
}
