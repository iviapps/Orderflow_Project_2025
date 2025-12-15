import { useEffect } from "react";
import { api } from "../../../lib/api";

export function ProductsPage() {
  useEffect(() => {
    api.get("/api/v1/products")
      .then(r => console.log("products ok:", r.status))
      .catch(e => console.error("products error:", e));
  }, []);

  return <div>Products</div>;
}
