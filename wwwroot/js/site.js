// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    function asOptions(select) {
        return Array.from(select.options || []);
    }

    function getNaOption(options) {
        return options.find(function (option) {
            const text = (option.textContent || "").trim().toLowerCase();
            const value = String(option.value || "").trim().toLowerCase();
            return text === "n/a" || value === "n/a";
        }) || null;
    }

    function shouldUseEmptyNaValue(select, options) {
        const nameOrId = (select.getAttribute("name") || select.id || "").toLowerCase();
        const fieldToken = nameOrId.replace(/\]/g, "").split(/[.\[]/).pop() || nameOrId;
        const isIdentifierSelect = fieldToken === "id" ||
            fieldToken === "assetid" ||
            fieldToken.endsWith("id") ||
            fieldToken.endsWith("ids");
        const isBooleanSelect = options.some(function (option) { return option.value === "true"; }) &&
            options.some(function (option) { return option.value === "false"; });

        return isIdentifierSelect || isBooleanSelect;
    }

    function dispatchInputChange(element) {
        element.dispatchEvent(new Event("input", { bubbles: true }));
        element.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function addNaOption(select) {
        const options = asOptions(select);
        const selectedOption = select.selectedOptions && select.selectedOptions.length > 0
            ? select.selectedOptions[0]
            : null;
        let option = getNaOption(options);

        if (!option) {
            option = document.createElement("option");
            option.textContent = "N/A";
            option.value = select.dataset.naValue || (shouldUseEmptyNaValue(select, options) ? "" : "N/A");
        }

        if (select.firstElementChild !== option) {
            select.insertBefore(option, select.firstElementChild);
        }

        if (selectedOption && selectedOption !== option) {
            selectedOption.selected = true;
        }
    }

    function addDateNaControl(input) {
        if (input.dataset.naControlAttached === "true") {
            return;
        }

        const button = document.createElement("button");
        button.type = "button";
        button.className = "vector-date-na";
        button.textContent = "N/A";
        button.setAttribute("aria-label", "Clear date as not applicable");
        button.addEventListener("click", function () {
            input.value = "";
            dispatchInputChange(input);
        });

        input.dataset.naControlAttached = "true";
        input.insertAdjacentElement("afterend", button);
    }

    const vectorDateStatusClasses = [
        "vector-date-status",
        "vector-date-green",
        "vector-date-amber",
        "vector-date-orange",
        "vector-date-dark-orange",
        "vector-date-red"
    ];

    function clearDateStatus(element) {
        if (element.dataset.vectorDateStatusApplied !== "true") {
            return;
        }

        element.classList.remove(...vectorDateStatusClasses);
        element.removeAttribute("data-vector-date-status-applied");
        element.removeAttribute("data-vector-date-status");
        const originalTitle = element.dataset.vectorDateOriginalTitle || "";
        if (originalTitle) {
            element.setAttribute("title", originalTitle);
        } else {
            element.removeAttribute("title");
        }
        element.removeAttribute("data-vector-date-original-title");
    }

    function parseDateParts(year, month, day) {
        const parsedYear = Number(year);
        const parsedMonth = Number(month);
        const parsedDay = Number(day);

        if (!parsedYear || !parsedMonth || !parsedDay) {
            return null;
        }

        const date = new Date(parsedYear, parsedMonth - 1, parsedDay);
        if (date.getFullYear() !== parsedYear ||
            date.getMonth() !== parsedMonth - 1 ||
            date.getDate() !== parsedDay) {
            return null;
        }

        return date;
    }

    function parseDateValue(value) {
        const text = String(value || "").trim();
        if (!text ||
            /^n\/a$/i.test(text) ||
            /^(not set|not recorded|no expiry|manager-selected expiry)$/i.test(text)) {
            return null;
        }

        const isoMatches = Array.from(text.matchAll(/\b(\d{4})[-/](\d{1,2})[-/](\d{1,2})\b/g));
        const slashMatches = Array.from(text.matchAll(/\b(\d{1,2})\/(\d{1,2})\/(\d{4})\b/g));
        const totalMatches = isoMatches.length + slashMatches.length;
        if (totalMatches !== 1) {
            return null;
        }

        if (isoMatches.length === 1) {
            return parseDateParts(isoMatches[0][1], isoMatches[0][2], isoMatches[0][3]);
        }

        return parseDateParts(slashMatches[0][3], slashMatches[0][2], slashMatches[0][1]);
    }

    function normalizeDateContext(value) {
        return String(value || "")
            .replace(/([a-z])([A-Z])/g, "$1 $2")
            .replace(/[_\-.]+/g, " ")
            .replace(/\s+/g, " ")
            .trim()
            .toLowerCase();
    }

    function textFromClosestLabel(element) {
        const explicitId = element.id;
        if (explicitId) {
            const explicitLabel = document.querySelector(`label[for='${CSS.escape(explicitId)}']`);
            if (explicitLabel) {
                return explicitLabel.textContent || "";
            }
        }

        const wrappingLabel = element.closest("label");
        return wrappingLabel ? (wrappingLabel.textContent || "") : "";
    }

    function textFromTableHeader(element) {
        const cell = element.closest("td, th");
        const row = cell?.parentElement;
        const table = cell?.closest("table");
        if (!cell || !row || !table) {
            return "";
        }

        const cells = Array.from(row.children).filter(function (child) {
            return child.matches("td, th");
        });
        const index = cells.indexOf(cell);
        if (index < 0) {
            return "";
        }

        const headerRow = table.querySelector("thead tr");
        const headers = headerRow
            ? Array.from(headerRow.children).filter(function (child) { return child.matches("td, th"); })
            : [];

        return headers[index]?.textContent || "";
    }

    function dateStatusContextFor(element) {
        const parts = [
            element.dataset.dateStatusScope,
            element.dataset.dateStatusField,
            element.getAttribute("name"),
            element.id,
            element.getAttribute("aria-label"),
            element.getAttribute("placeholder"),
            element.getAttribute("title"),
            textFromClosestLabel(element),
            textFromTableHeader(element)
        ];

        let current = element.parentElement;
        for (let depth = 0; current && depth < 3; depth += 1) {
            parts.push(current.dataset?.dateStatusScope);
            parts.push(current.dataset?.dateStatusField);
            parts.push(current.getAttribute?.("aria-label"));

            if (current.matches?.(".profile-field, .detail-row, .metric-row, .register-row, .asset-row, .table-row, .form-group, .field, .readonly-field, .summary-field, .report-field")) {
                parts.push(current.textContent || "");
            }

            current = current.parentElement;
        }

        return normalizeDateContext(parts.filter(Boolean).join(" "));
    }

    function isServiceExpiryLicenseOrCpdDate(element) {
        if (element.dataset.skipDateStatus === "true") {
            return false;
        }

        const explicit = normalizeDateContext(element.dataset.dateStatusScope || element.dataset.dateStatusField || "");
        if (explicit === "service" ||
            explicit === "expiry" ||
            explicit === "expiration" ||
            explicit === "license" ||
            explicit === "licence" ||
            explicit === "cpd") {
            return true;
        }

        const context = dateStatusContextFor(element);
        if (!context) {
            return false;
        }

        if (/\b(last service|previous service|service history)\b/.test(context)) {
            return false;
        }

        if (/\b(cpd|continuing professional development)\b/.test(context)) {
            return true;
        }

        if (/\b(licence|license|licensing|licencing|licence disc|license disc|practitioner licence|practitioner license)\b/.test(context)) {
            return true;
        }

        if (/\b(expiry|expires|expired|expiration|use by|use-by|best before|valid until)\b/.test(context)) {
            return true;
        }

        return /\b(next service|service due|due for service|service expiry|service expiration|service pressure|service date)\b/.test(context);
    }

    function getDateStatus(date) {
        const now = new Date();
        const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        const target = new Date(date.getFullYear(), date.getMonth(), date.getDate());
        const daysUntil = Math.round((target.getTime() - today.getTime()) / 86400000);

        if (daysUntil < 0) {
            return { className: "vector-date-red", label: "Past due" };
        }

        if (daysUntil <= 15) {
            return { className: "vector-date-dark-orange", label: "Due within 15 days" };
        }

        if (daysUntil <= 30) {
            return { className: "vector-date-orange", label: "Due within 30 days" };
        }

        if (daysUntil <= 60) {
            return { className: "vector-date-amber", label: "Due within 60 days" };
        }

        return { className: "vector-date-green", label: "More than 60 days" };
    }

    function applyDateStatus(element, date) {
        const status = getDateStatus(date);
        clearDateStatus(element);
        element.dataset.vectorDateOriginalTitle = element.getAttribute("title") || "";
        element.classList.add("vector-date-status", status.className);
        element.dataset.vectorDateStatusApplied = "true";
        element.dataset.vectorDateStatus = status.label;

        element.setAttribute("title", status.label);
    }

    function initializeDateStatusColors(root) {
        const scope = root || document;

        scope.querySelectorAll("input[type='date']:not([data-skip-date-status]), input[type='datetime-local']:not([data-skip-date-status])").forEach(function (input) {
            const refresh = function () {
                if (!isServiceExpiryLicenseOrCpdDate(input)) {
                    clearDateStatus(input);
                    return;
                }

                const date = parseDateValue(input.value);
                if (date) {
                    applyDateStatus(input, date);
                } else {
                    clearDateStatus(input);
                }
            };

            if (input.dataset.vectorDateStatusBound !== "true") {
                input.dataset.vectorDateStatusBound = "true";
                input.addEventListener("input", refresh);
                input.addEventListener("change", refresh);
            }

            refresh();
        });

        scope.querySelectorAll("td, th, span, small, strong, em, p, div, li").forEach(function (element) {
            if (element.dataset.skipDateStatus === "true" ||
                element.closest("script, style, svg, .vector-date-na") ||
                element.children.length > 0) {
                return;
            }

            const text = (element.textContent || "").trim();
            if (text.length > 80) {
                clearDateStatus(element);
                return;
            }

            const date = parseDateValue(text);
            if (date && isServiceExpiryLicenseOrCpdDate(element)) {
                applyDateStatus(element, date);
            } else {
                clearDateStatus(element);
            }
        });
    }

    window.VectorInitializeDateStatusColors = initializeDateStatusColors;

    function ensureUniversalNaControls() {
        document.querySelectorAll("select:not([data-skip-na])").forEach(function (select) {
            addNaOption(select);
        });

        document.querySelectorAll("input[type='date']:not([data-skip-na-control]), input[type='datetime-local']:not([data-skip-na-control])").forEach(function (input) {
            addDateNaControl(input);
        });
    }

    ensureUniversalNaControls();
    window.VectorEnsureUniversalNaControls = ensureUniversalNaControls;

    function hasLocalSectionToggle(target) {
        return Boolean(target?.matches(".checklist-section-editor") &&
            target.querySelector(":scope > .visual-section-heading [data-action='toggle-section']"));
    }

    function syncLocalSectionToggle(target) {
        const toggle = target?.querySelector(":scope > .visual-section-heading [data-action='toggle-section']");
        if (!toggle) {
            return;
        }

        const isCollapsed = target.classList.contains("is-collapsed");
        toggle.textContent = isCollapsed ? "Open Section" : "Close Section";
        toggle.setAttribute("aria-expanded", String(!isCollapsed));
    }

    function collapseTarget(target, collapse, expandedLabel, collapsedLabel) {
        if (!target) {
            return;
        }

        if (hasLocalSectionToggle(target)) {
            target.classList.toggle("is-collapsed", Boolean(collapse));
            syncLocalSectionToggle(target);
        }

        target.classList.toggle("vector-collapsed", Boolean(collapse));
        target.querySelectorAll("[data-vector-collapse-toggle]").forEach(function (button) {
            const nestedTarget = button.closest(".checklist-section-editor, .section-card, .equipment-row-config, .builder-field");
            if (nestedTarget === target) {
                button.textContent = target.classList.contains("vector-collapsed") ? collapsedLabel : expandedLabel;
                button.setAttribute("aria-expanded", String(!target.classList.contains("vector-collapsed")));
            }
        });
    }

    function createCollapseButton(target, expandedLabel, collapsedLabel) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "vector-collapse-toggle";
        button.dataset.vectorCollapseToggle = "true";
        button.textContent = target.classList.contains("vector-collapsed") ? collapsedLabel : expandedLabel;
        button.setAttribute("aria-expanded", String(!target.classList.contains("vector-collapsed")));
        button.addEventListener("click", function (event) {
            event.preventDefault();
            event.stopPropagation();
            collapseTarget(target, !target.classList.contains("vector-collapsed"), expandedLabel, collapsedLabel);
        });
        return button;
    }

    function appendCollapseButton(target, toolbar, expandedLabel, collapsedLabel) {
        if (!target || !toolbar || target.dataset.vectorCollapseAttached === "true") {
            return;
        }

        if (hasLocalSectionToggle(target)) {
            target.dataset.vectorCollapseAttached = "true";
            syncLocalSectionToggle(target);
            return;
        }

        target.dataset.vectorCollapseAttached = "true";
        toolbar.appendChild(createCollapseButton(target, expandedLabel, collapsedLabel));
    }

    function initializeSectionCollapses(root) {
        (root || document).querySelectorAll(".checklist-section-editor, .section-card").forEach(function (section) {
            const toolbar = section.querySelector(":scope > .visual-section-heading .section-controls") ||
                section.querySelector(":scope > .section-header");
            appendCollapseButton(section, toolbar, "Collapse Section", "Open Section");
        });
    }

    function initializeItemCollapses(root) {
        (root || document).querySelectorAll(".equipment-row-config, .builder-field").forEach(function (item) {
            const toolbar = item.querySelector(":scope > .row-config-toolbar > div") ||
                item.querySelector(":scope > .field-toolbar > div");

            if (item.dataset.vectorCollapseInitialised !== "true") {
                item.classList.add("vector-collapsed");
                item.dataset.vectorCollapseInitialised = "true";
            }

            appendCollapseButton(item, toolbar, "Collapse", "Edit");
        });
    }

    function initializeBuilderBulkCollapse(root) {
        (root || document).querySelectorAll(".layout-builder").forEach(function (builder) {
            if (builder.dataset.vectorBulkCollapseAttached === "true") {
                return;
            }

            const toolbar = builder.querySelector(".builder-toolbar .toolbar-actions");
            if (!toolbar) {
                return;
            }

            builder.dataset.vectorBulkCollapseAttached = "true";

            const actionWrap = document.createElement("div");
            actionWrap.className = "vector-collapse-actions";

            const collapseSectionsButton = document.createElement("button");
            collapseSectionsButton.type = "button";
            collapseSectionsButton.textContent = "Collapse Sections";
            collapseSectionsButton.addEventListener("click", function () {
                builder.querySelectorAll(".checklist-section-editor").forEach(function (section) {
                    collapseTarget(section, true, "Collapse Section", "Open Section");
                });
            });

            const expandSectionsButton = document.createElement("button");
            expandSectionsButton.type = "button";
            expandSectionsButton.textContent = "Expand Sections";
            expandSectionsButton.addEventListener("click", function () {
                builder.querySelectorAll(".checklist-section-editor").forEach(function (section) {
                    collapseTarget(section, false, "Collapse Section", "Open Section");
                });
            });

            const collapseItemsButton = document.createElement("button");
            collapseItemsButton.type = "button";
            collapseItemsButton.textContent = "Collapse Items";
            collapseItemsButton.addEventListener("click", function () {
                builder.querySelectorAll(".equipment-row-config, .builder-field").forEach(function (item) {
                    collapseTarget(item, true, "Collapse", "Edit");
                });
            });

            const expandItemsButton = document.createElement("button");
            expandItemsButton.type = "button";
            expandItemsButton.textContent = "Expand Items";
            expandItemsButton.addEventListener("click", function () {
                builder.querySelectorAll(".equipment-row-config, .builder-field").forEach(function (item) {
                    collapseTarget(item, false, "Collapse", "Edit");
                });
            });

            actionWrap.append(collapseSectionsButton, expandSectionsButton, collapseItemsButton, expandItemsButton);
            toolbar.appendChild(actionWrap);
        });
    }

    function initializeChecklistCollapsibles(root) {
        initializeBuilderBulkCollapse(root);
        initializeSectionCollapses(root);
        initializeItemCollapses(root);
    }

    window.VectorInitializeChecklistCollapsibles = initializeChecklistCollapsibles;

    function isActionConfirmationMessage(element) {
        if (!element || element.dataset.persistentStatus === "true") {
            return false;
        }

        if (element.matches(".confirmation-flag, [data-transient-confirmation], [data-action-status]")) {
            return true;
        }

        const text = (element.textContent || "").trim().toLowerCase();
        if (!text) {
            return false;
        }

        const errorTokens = [
            "enter ",
            "select ",
            "no ",
            "only ",
            "unsupported",
            "not found",
            "missing",
            "could not",
            "must ",
            "required",
            "before saving",
            "before submitting"
        ];

        if (errorTokens.some(function (token) { return text.includes(token); })) {
            return false;
        }

        const actionTokens = [
            "saved",
            "deleted",
            "removed",
            "submitted",
            "sent",
            "published",
            "uploaded",
            "created",
            "updated",
            "approved",
            "completed",
            "confirmed",
            "moved",
            "reassigned",
            "copied",
            "authorised",
            "authorized",
            "marked",
            "allocated",
            "entered"
        ];

        return actionTokens.some(function (token) { return text.includes(token); });
    }

    function initializeTransientActionFeedback(root) {
        (root || document)
            .querySelectorAll(".confirmation-flag, .status-message, [data-transient-confirmation], [data-action-status]")
            .forEach(function (element) {
                if (element.dataset.vectorTransientAttached === "true" || !isActionConfirmationMessage(element)) {
                    return;
                }

                element.dataset.vectorTransientAttached = "true";
                element.classList.add("vector-transient-action-feedback");

                const duration = Number(element.dataset.confirmationDuration || 3400);
                window.setTimeout(function () {
                    element.classList.add("vector-transient-action-feedback-gone");
                    element.setAttribute("aria-hidden", "true");

                    window.setTimeout(function () {
                        element.hidden = true;
                    }, 260);
                }, duration);
            });
    }

    window.VectorInitializeTransientActionFeedback = initializeTransientActionFeedback;

    window.VectorShiftDrafts = window.VectorShiftDrafts || {
        activeDrafts: {},
        clear: function (key) {
            const storageKey = String(key || "").startsWith("vectorShiftDraft:")
                ? String(key || "")
                : "vectorShiftDraft:" + String(key || "");

            if (this.activeDrafts[storageKey]) {
                this.activeDrafts[storageKey].clear();
                return;
            }

            localStorage.removeItem(storageKey);
        },
        bindForm: function (form, options) {
            const settings = options || {};
            const storageKey = "vectorShiftDraft:" + (settings.key || window.location.pathname);
            const ttlMilliseconds = Number(settings.ttlMilliseconds || 12 * 60 * 60 * 1000);
            const statusElement = settings.statusElement || null;
            const fields = Array.from(form?.querySelectorAll("input[name], select[name], textarea[name]") || []);
            let saveTimer = null;
            let disabled = false;

            if (!form || fields.length === 0) {
                return;
            }

            this.activeDrafts[storageKey] = {
                clear: function () {
                    disabled = true;
                    window.clearTimeout(saveTimer);
                    localStorage.removeItem(storageKey);
                    writeStatus("Saved. A fresh check will open next.");
                }
            };

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
                if (disabled) {
                    return;
                }

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

    (window.VectorDraftKeysToClear || []).forEach(function (key) {
        window.VectorShiftDrafts.clear(key);
    });
    window.VectorDraftKeysToClear = [];

    function optionLooksLikeOther(select) {
        const selectedOption = select.options[select.selectedIndex];
        const value = String(select.value || "").trim().toLowerCase();
        const text = String(selectedOption?.textContent || "").trim().toLowerCase();
        return value === "__other" || value === "other" || text === "other";
    }

    function syncOtherOptionField(select) {
        const key = select.dataset.otherSelect;
        if (!key) {
            return;
        }

        const field = document.querySelector("[data-other-field='" + key + "']");
        if (!field) {
            return;
        }

        const input = field.querySelector("input, textarea");
        const show = optionLooksLikeOther(select);
        field.classList.toggle("is-hidden", !show);
        field.hidden = !show;
        if (input) {
            input.required = show;
            if (!show) {
                input.value = "";
            }
        }
    }

    function initializeOtherOptionFields(root) {
        (root || document).querySelectorAll("[data-other-select]").forEach(function (select) {
            if (select.dataset.otherOptionBound !== "true") {
                select.dataset.otherOptionBound = "true";
                select.addEventListener("change", function () {
                    syncOtherOptionField(select);
                });
            }

            syncOtherOptionField(select);
        });
    }

    window.VectorInitializeOtherOptionFields = initializeOtherOptionFields;

    document.addEventListener("DOMContentLoaded", function () {
        ensureUniversalNaControls();
        initializeDateStatusColors(document);
        initializeChecklistCollapsibles(document);
        initializeTransientActionFeedback(document);
        initializeOtherOptionFields(document);

        let collapseRefreshQueued = false;
        const collapseObserver = new MutationObserver(function () {
            if (collapseRefreshQueued) {
                return;
            }

            collapseRefreshQueued = true;
            window.requestAnimationFrame(function () {
                initializeChecklistCollapsibles(document);
                ensureUniversalNaControls();
                initializeDateStatusColors(document);
                initializeTransientActionFeedback(document);
                initializeOtherOptionFields(document);
                collapseRefreshQueued = false;
            });
        });

        collapseObserver.observe(document.body, { childList: true, subtree: true });

        document.addEventListener("click", function (event) {
            const moveTrigger = event.target.closest("[data-move-asset]");
            if (moveTrigger) {
                const assetType = String(moveTrigger.dataset.moveAsset || "").trim().toLowerCase();
                const allowedAssetTypes = ["vehicle", "equipment", "stock", "medication"];
                if (allowedAssetTypes.includes(assetType)) {
                    event.preventDefault();
                    window.location.href = "/MoveAsset?asset=" + encodeURIComponent(assetType);
                    return;
                }
            }

            const moveCard = event.target.closest("a.module-card, a.category-action, a.workflow-action");
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
        const issueNotification = document.getElementById("issueNotification");
        const issueCount = document.getElementById("issueCount");
        const varianceNotification = document.getElementById("varianceNotification");
        const varianceCount = document.getElementById("varianceCount");
        const readinessAlertNotification = document.getElementById("readinessAlertNotification");
        const readinessAlertCount = document.getElementById("readinessAlertCount");
        const expiryNotification = document.getElementById("expiryNotification");
        const expiryCount = document.getElementById("expiryCount");
        const scoringNotification = document.getElementById("scoringNotification");
        const scoringCount = document.getElementById("scoringCount");

        if (taskNotification && taskCount) {
            fetch("/TaskNotificationCount", { cache: "no-store" })
                .then(response => response.ok ? response.json() : { count: 0 })
                .then(data => {
                    const count = Number(data.count || 0);
                    if (count > 0) {
                        taskCount.textContent = String(count);
                        if (data.url) {
                            taskNotification.href = data.url;
                        }
                        if (data.label) {
                            taskNotification.title = data.label;
                            taskNotification.setAttribute("aria-label", data.label);
                        }
                        taskNotification.classList.add("visible");
                    }
                })
                .catch(() => {
                    // Keep the UI quiet if the count endpoint is unavailable.
                });
        }

        if (issueNotification && issueCount) {
            fetch("/IssueNotificationCount", { cache: "no-store" })
                .then(response => response.ok ? response.json() : { count: 0 })
                .then(data => {
                    const count = Number(data.count || 0);
                    if (count > 0) {
                        issueCount.textContent = String(count);
                        issueNotification.classList.add("visible");
                    }
                })
                .catch(() => {
                    // Keep the UI quiet if the count endpoint is unavailable.
                });
        }

        if (varianceNotification && varianceCount) {
            fetch("/ChecklistVarianceNotificationCount", { cache: "no-store" })
                .then(response => response.ok ? response.json() : { count: 0 })
                .then(data => {
                    const count = Number(data.count || 0);
                    if (count > 0) {
                        varianceCount.textContent = String(count);
                        varianceNotification.classList.add("visible");
                    }
                })
                .catch(() => {
                    // Keep the UI quiet if the count endpoint is unavailable.
                });
        }

        if (readinessAlertNotification && readinessAlertCount) {
            fetch("/ReadinessAlertNotificationCount", { cache: "no-store" })
                .then(response => response.ok ? response.json() : { count: 0 })
                .then(data => {
                    const count = Number(data.count || 0);
                    if (count > 0) {
                        readinessAlertCount.textContent = String(count);
                        readinessAlertNotification.classList.add("visible");
                    }
                })
                .catch(() => {
                    // Keep the UI quiet if the count endpoint is unavailable.
                });
        }

        if (expiryNotification && expiryCount) {
            fetch("/ExpiryNotificationCount", { cache: "no-store" })
                .then(response => response.ok ? response.json() : { count: 0 })
                .then(data => {
                    const count = Number(data.count || 0);
                    if (count > 0) {
                        expiryCount.textContent = String(count);
                        if (data.url) {
                            expiryNotification.href = data.url;
                        }
                        if (data.label) {
                            expiryNotification.title = data.label;
                            expiryNotification.setAttribute("aria-label", data.label);
                        }
                        expiryNotification.classList.add("visible");
                    }
                })
                .catch(() => {
                    // Keep the UI quiet if the count endpoint is unavailable.
                });
        }

        if (scoringNotification && scoringCount) {
            fetch("/ReadinessScoringRequestNotificationCount", { cache: "no-store" })
                .then(response => response.ok ? response.json() : { count: 0 })
                .then(data => {
                    const count = Number(data.count || 0);
                    if (count > 0) {
                        scoringCount.textContent = String(count);
                        scoringNotification.classList.add("visible");
                    }
                })
                .catch(() => {
                    // Keep the UI quiet if the count endpoint is unavailable.
                });
        }
    });
})();
