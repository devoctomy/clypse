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

                // For the weak passwords file, split it into lines for more efficient access
                if (url.includes('weakknownpasswords.txt')) {
                    window.__bootData[url] = body.split(/\r?\n/).filter(line => line.trim().length > 0);
                    console.log(`[Prefetch] Cached ${url} as array (${window.__bootData[url].length} lines)`);
                } else {
                    window.__bootData[url] = body;
                    console.log(`[Prefetch] Cached ${url} (${body.length ?? 'unknown'} bytes)`);
                }
            } catch (err) {
                console.error(`[Prefetch] Error fetching ${url}`, err);
            }
        }));

        console.log("[Prefetch] All boot files fetched and cached.");
    }

    // kick off immediately, and hang Blazor boot until done
    window.__prefetchPromise = prefetchBootData();

    // Fast accessor function for Blazor to get prefetched data
    window.getBootData = function (path) {
        console.log(`[getBootData] Requested: ${path}`);
        const data = window.__bootData[path] || null;
        console.log(`[getBootData] Found data: ${data ? 'yes' : 'no'} (${Array.isArray(data) ? data.length + ' lines' : data?.length + ' chars'})`);
        return data;
    };

    // Get count of lines for weak passwords
    window.getBootDataCount = function (path) {
        const data = window.__bootData[path] || null;
        return Array.isArray(data) ? data.length : 0;
    };

    // Synchronous check if data is available
    window.hasBootData = function (path) {
        return window.__bootData && window.__bootData[path] !== undefined;
    };
})();