import express from "express";

// RailBite is the external catering platform. It shares no code or database with SmartRail
// and is reached only over HTTP (signed webhooks and API keys), so it can be down independently.
const app = express();
app.use(express.json());

app.get("/health", (_req, res) => {
  res.json({ status: "ok", service: "railbite-catering" });
});

const port = Number(process.env.PORT ?? 4000);
app.listen(port, () => {
  console.log(`RailBite catering listening on ${port}`);
});
