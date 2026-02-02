const gatewayUrl =
    import.meta.env.VITE_API_GATEWAY_URL ?? "";

export const config = {
    apiBaseUrl: gatewayUrl,
    apiPrefix: "/api/v1",
};

if (!gatewayUrl) {
    console.warn("API Gateway URL not defined");
}
