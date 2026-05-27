// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    window.VectorShiftDrafts = window.VectorShiftDrafts || {
        bindForm: function (form, options) {
            const settings = options || {};
            const storageKey = "vectorShiftDraft:" + (settings.key || window.location.pathname);
            const ttlMilliseconds = Number(settings.ttlMilliseconds || 12 * 60 * 60 * 1000);
            const statusElement = settings.statusElement || null;
            const fields = Array.from(form?.querySelectorAll("input[name], select[name], textarea[name]") || []);
            let saveTimer = null;

            if (!form || fields.length === 0) {
                return;
            }

            function readDraft() {
                try {
                    const rawDraft = localStorage.getItem(storageKey);
                    return rawDraft ? JSON.parse(rawDraft) : null;
                } catch {
                    return null;
                }
            }

            function writeStatus(message) {
                if (statusElement) {
                    statusElement.textContent = message;
                }
            }

            function captureField(field) {
                if (field.type === "checkbox" || field.type === "radio") {
                    return field.checked;
                }

                return field.value;
            }

            function restoreField(field, value) {
                if (value === undefined || value === null) {
                    return;
                }

                if (field.type === "checkbox" || field.type === "radio") {
                    field.checked = Boolean(value);
                    return;
                }

                field.value = String(value);
            }

            function saveDraft() {
                const existingDraft = readDraft();
                const now = Date.now();
                const expiresAt = existingDraft?.expiresAt && existingDraft.expiresAt > now
                    ? existingDraft.expiresAt
                    : now + ttlMilliseconds;
                const values = {};

                fields.forEach(function (field) {
                    values[field.name] = captureField(field);
                });

                localStorage.setItem(storageKey, JSON.stringify({
                    expiresAt: expiresAt,
                    savedAt: now,
                    values: values
                }));

                writeStatus("Progress saved locally for this shift.");
            }

            function scheduleSave() {
                window.clearTimeout(saveTimer);
                saveTimer = window.setTimeout(saveDraft, 250);
            }

            const draft = readDraft();
            if (draft?.expiresAt && draft.expiresAt > Date.now() && draft.values) {
                fields.forEach(function (field) {
                    restoreField(field, draft.values[field.name]);
                });
                writeStatus("Draft restored. Progress is kept locally until the shift expires.");
                if (typeof settings.onAfterRestore === "function") {
                    settings.onAfterRestore();
                }
            } else if (draft) {
                localStorage.removeItem(storageKey);
                writeStatus("Previous draft expired. Fresh check started.");
            } else {
                writeStatus("Progress saves locally for 12 hours during this shift.");
            }

            fields.forEach(function (field) {
                field.addEventListener("input", scheduleSave);
                field.addEventListener("change", scheduleSave);
            });

            window.addEventListener("beforeunload", saveDraft);
        }
    };

    (window.VectorPendingShiftDraftBindings || []).forEach(function (bindDraft) {
        if (typeof bindDraft === "function") {
            bindDraft();
        }
    });
    window.VectorPendingShiftDraftBindings = [];

    document.addEventListener("DOMContentLoaded", function () {
        document.addEventListener("click", function (event) {
            const moveCard = event.target.closest("a.module-card");
            if (!moveCard) {
                return;
            }

            const label = (moveCard.textContent || "").toLowerCase();
            const href = moveCard.getAttribute("href") || "";
            if (!label.includes("move / reallocate")) {
                return;
            }

            const path = window.location.pathname.toLowerCase();
            const fallbackRoutes = {
                "/vehicles": "/MoveAsset?asset=vehicle",
                "/equipment": "/MoveAsset?asset=equipment",
                "/stock": "/MoveAsset?asset=stock",
                "/medication": "/MoveAsset?asset=medication"
            };
            const fallbackRoute = fallbackRoutes[path];

            if (fallbackRoute && (href === "#" || href.endsWith("#"))) {
                event.preventDefault();
                window.location.href = fallbackRoute;
            }
        });

        const taskNotification = document.getElementById("taskNotification");
        const taskCount = document.getElementById("taskCount");

        if (!taskNotification || !taskCount) {
            return;
        }

        fetch("/TaskNotificationCount", { cache: "no-store" })
            .then(response => response.ok ? response.json() : { count: 0 })
            .then(data => {
                const count = Number(data.count || 0);
                if (count > 0) {
                    taskCount.textContent = String(count);
                    taskNotification.classList.add("visible");
                }
            })
            .catch(() => {
                // Keep the UI quiet if the count endpoint is unavailable.
            });
    });
})();
