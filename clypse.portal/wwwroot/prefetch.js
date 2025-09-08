// wwwroot/prefetch.js
(function () {
    const bootFiles = [
        '/data/dictionaries/weakknownpasswords.txt',
    ];

    window.__bootData = {};

    async function prefetchBootData() {
        console.log("[Prefetch] Starting prefetch of boot files:", bootFiles);

        await Promise.all(bootFiles.map(async (url) => {
            try {
                console.log(`[Prefetch] Fetching: ${url}`);
                const res = await fetch(url, { cache: 'force-cache' });

                if (!res.ok) {
                    console.error(`[Prefetch] Failed to fetch ${url}: ${res.status}`);
                    return;
                }

                const body = url.endsWith('.json')
                    ? await res.json()
                    : await res.text();

                window.__bootData[url] = body;
                console.log(`[Prefetch] Cached ${url} (${body.length ?? 'unknown'} bytes)`);
            } catch (err) {
                console.error(`[Prefetch] Error fetching ${url}`, err);
            }
        }));

        console.log("[Prefetch] All boot files fetched and cached.");
    }

    // kick off immediately, and hang Blazor boot until done
    window.__prefetchPromise = prefetchBootData();
})();