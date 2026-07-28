window.wattWeather = {
    share: async (title, text, url) => {
        if (navigator.share) {
            try {
                await navigator.share({ title, text, url });
                return "shared";
            } catch (error) {
                if (error?.name === "AbortError") return "cancelled";
                throw error;
            }
        }
        await navigator.clipboard.writeText(url);
        return "copied";
    }
};
