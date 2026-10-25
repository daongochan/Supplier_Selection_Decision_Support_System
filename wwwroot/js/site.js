/**
 * SupplierSelectionDSS - Enterprise Client Script
 */

(function () {
    "use strict";

    // =========================================================
    // GLOBAL DSS UTILITIES & CONFIRMATION MODAL
    // =========================================================
    window.DSS = {
        _confirmCallback: null,
        _previousActiveElement: null,
        _isProcessing: false,
        _currentConfirmText: "Confirm",

        showConfirmModal: function (options) {
            const modal = document.getElementById("dssConfirmModal");
            if (!modal) {
                if (confirm(options.message || "Are you sure you want to proceed with this action?")) {
                    if (typeof options.onConfirm === "function") {
                        options.onConfirm("");
                    }
                }
                return;
            }

            this._previousActiveElement = document.activeElement;
            this._isProcessing = false;

            const titleEl = document.getElementById("dssConfirmTitle");
            const messageEl = document.getElementById("dssConfirmMessage");
            const iconBadge = document.getElementById("dssConfirmIconBadge");
            const iconEl = document.getElementById("dssConfirmIcon");
            const submitBtn = document.getElementById("dssConfirmSubmitBtn");
            const submitTextEl = document.getElementById("dssConfirmSubmitText");
            const submitSpinnerEl = document.getElementById("dssConfirmSubmitSpinner");
            const cancelBtn = document.getElementById("dssConfirmCancelBtn");
            const closeBtn = document.getElementById("dssConfirmCloseBtn");
            const consequenceBox = document.getElementById("dssConfirmConsequence");
            const consequenceText = document.getElementById("dssConfirmConsequenceText");
            const inputGroup = document.getElementById("dssConfirmInputGroup");
            const inputLabel = document.getElementById("dssConfirmInputLabel");
            const inputEl = document.getElementById("dssConfirmInput");

            // Reset loading and disabled states
            if (submitSpinnerEl) submitSpinnerEl.style.display = "none";
            if (submitBtn) submitBtn.disabled = false;
            if (cancelBtn) cancelBtn.disabled = false;
            if (closeBtn) closeBtn.disabled = false;

            // Content bindings
            this._currentConfirmText = options.confirmText || "Confirm";
            if (titleEl) titleEl.textContent = options.title || "Confirm Action";
            if (messageEl) messageEl.textContent = options.message || "Are you sure you want to continue?";
            if (submitTextEl) {
                submitTextEl.textContent = this._currentConfirmText;
            }

            // Consequence callout - only show when non-empty text exists
            if (options.consequence && options.consequence.trim() && consequenceBox && consequenceText) {
                consequenceText.textContent = options.consequence.trim();
                consequenceBox.classList.add("is-visible");
            } else if (consequenceBox) {
                consequenceBox.classList.remove("is-visible");
                if (consequenceText) consequenceText.textContent = "";
            }

            // Semantic action type & styles
            let actionType = options.actionType || "primary";
            let btnClass = "btn-primary";
            let iconClass = "bi bi-question-circle-fill";

            if (options.confirmBtnClass) {
                btnClass = options.confirmBtnClass;
            } else if (actionType === "danger") {
                btnClass = "btn-danger";
                iconClass = "bi bi-exclamation-triangle-fill";
            } else if (actionType === "warning") {
                btnClass = "btn-warning";
                iconClass = "bi bi-arrow-counterclockwise";
            } else if (actionType === "success") {
                btnClass = "btn-success";
                iconClass = "bi bi-check-circle-fill";
            }

            if (iconBadge) {
                iconBadge.className = "dss-modal-icon-badge badge-" + actionType;
            }
            if (iconEl) iconEl.className = iconClass;
            if (submitBtn) submitBtn.className = "dss-modal-btn dss-modal-btn-confirm " + btnClass;

            // Optional text input (reasons / notes)
            if (options.requireInput) {
                if (inputGroup) inputGroup.style.display = "block";
                if (inputLabel) inputLabel.textContent = options.inputLabel || "Reason / Notes:";
                if (inputEl) {
                    inputEl.value = "";
                    inputEl.placeholder = options.inputPlaceholder || "Enter your reason or comments...";
                }
            } else {
                if (inputGroup) inputGroup.style.display = "none";
            }

            this._confirmCallback = options.onConfirm || null;
            modal.style.display = "flex";
            modal.setAttribute("aria-hidden", "false");

            // Focus management
            if (options.requireInput && inputEl) {
                setTimeout(function () { inputEl.focus(); }, 100);
            } else if (submitBtn) {
                setTimeout(function () { submitBtn.focus(); }, 100);
            }
        },

        closeConfirmModal: function () {
            if (this._isProcessing) return; // Prevent closing while processing

            const modal = document.getElementById("dssConfirmModal");
            if (modal) {
                modal.style.display = "none";
                modal.setAttribute("aria-hidden", "true");
            }
            this._confirmCallback = null;
            this._isProcessing = false;

            // Restore focus
            if (this._previousActiveElement && typeof this._previousActiveElement.focus === "function") {
                try {
                    this._previousActiveElement.focus();
                } catch (e) {
                    // Ignore if element is no longer focusable
                }
            }
        },

        handleBackdropClick: function () {
            if (this._isProcessing) return;
            this.closeConfirmModal();
        }
    };

    document.addEventListener("DOMContentLoaded", function () {

        // =====================================================
        // COLLAPSIBLE SIDEBAR & RESPONSIVE MOBILE NAVIGATION
        // =====================================================
        const STORAGE_KEY = "dss_sidebar_collapsed";
        const MOBILE_BREAKPOINT = 900;
        const sidebarToggleBtn = document.getElementById("sidebarToggle");
        const desktopSidebarToggle = document.getElementById("desktopSidebarToggle");
        const desktopToggleIcon = document.getElementById("desktopSidebarToggleIcon");
        const sidebarBackdrop = document.getElementById("sidebarBackdrop");
        const appSidebar = document.getElementById("appSidebar");

        function isDesktop() {
            return window.innerWidth > MOBILE_BREAKPOINT;
        }

        function getStoredCollapseState() {
            try {
                return localStorage.getItem(STORAGE_KEY) === "true";
            } catch (e) {
                return false;
            }
        }

        function setStoredCollapseState(isCollapsed) {
            try {
                localStorage.setItem(STORAGE_KEY, isCollapsed ? "true" : "false");
            } catch (e) { }
        }

        function applyDesktopState(isCollapsed, persist) {
            if (isCollapsed) {
                document.documentElement.classList.add("sidebar-collapsed");
                if (desktopSidebarToggle) {
                    desktopSidebarToggle.setAttribute("aria-expanded", "false");
                    desktopSidebarToggle.setAttribute("aria-label", "Expand sidebar");
                    desktopSidebarToggle.setAttribute("title", "Expand sidebar");
                }
                if (desktopToggleIcon) {
                    desktopToggleIcon.className = "bi bi-chevron-right";
                }
                if (sidebarToggleBtn && isDesktop()) {
                    sidebarToggleBtn.setAttribute("aria-expanded", "false");
                    sidebarToggleBtn.setAttribute("aria-label", "Expand sidebar");
                    sidebarToggleBtn.setAttribute("title", "Expand sidebar");
                }
            } else {
                document.documentElement.classList.remove("sidebar-collapsed");
                if (desktopSidebarToggle) {
                    desktopSidebarToggle.setAttribute("aria-expanded", "true");
                    desktopSidebarToggle.setAttribute("aria-label", "Collapse sidebar");
                    desktopSidebarToggle.setAttribute("title", "Collapse sidebar");
                }
                if (desktopToggleIcon) {
                    desktopToggleIcon.className = "bi bi-chevron-left";
                }
                if (sidebarToggleBtn && isDesktop()) {
                    sidebarToggleBtn.setAttribute("aria-expanded", "true");
                    sidebarToggleBtn.setAttribute("aria-label", "Collapse sidebar");
                    sidebarToggleBtn.setAttribute("title", "Collapse sidebar");
                }
            }

            if (persist) {
                setStoredCollapseState(isCollapsed);
            }

            // Trigger window resize event after CSS transition to allow Chart.js and layouts to re-measure cleanly
            setTimeout(function () {
                window.dispatchEvent(new Event("resize"));
            }, 220);
        }

        function toggleDesktopSidebar() {
            const isCurrentlyCollapsed = document.documentElement.classList.contains("sidebar-collapsed");
            applyDesktopState(!isCurrentlyCollapsed, true);
        }

        function openMobileSidebar() {
            document.body.classList.add("sidebar-mobile-open");
            if (sidebarToggleBtn) {
                sidebarToggleBtn.setAttribute("aria-expanded", "true");
            }
            if (sidebarBackdrop) {
                sidebarBackdrop.setAttribute("aria-hidden", "false");
            }
            if (appSidebar) {
                const firstInteractive = appSidebar.querySelector("button, a");
                if (firstInteractive && typeof firstInteractive.focus === "function") {
                    setTimeout(function () { firstInteractive.focus(); }, 60);
                }
            }
        }

        function closeMobileSidebar() {
            document.body.classList.remove("sidebar-mobile-open");
            if (sidebarToggleBtn) {
                sidebarToggleBtn.setAttribute("aria-expanded", "false");
            }
            if (sidebarBackdrop) {
                sidebarBackdrop.setAttribute("aria-hidden", "true");
            }
        }

        function toggleMobileSidebar() {
            if (document.body.classList.contains("sidebar-mobile-open")) {
                closeMobileSidebar();
            } else {
                openMobileSidebar();
            }
        }

        // Initialize desktop state from localStorage
        if (isDesktop()) {
            applyDesktopState(getStoredCollapseState(), false);
        } else {
            document.documentElement.classList.remove("sidebar-collapsed");
        }

        // Desktop sidebar toggle button click (inside sidebar header)
        if (desktopSidebarToggle) {
            desktopSidebarToggle.addEventListener("click", function (e) {
                e.preventDefault();
                if (isDesktop()) {
                    toggleDesktopSidebar();
                } else {
                    closeMobileSidebar();
                }
            });
        }

        // Topbar toggle button click (hamburger)
        if (sidebarToggleBtn) {
            sidebarToggleBtn.addEventListener("click", function (e) {
                e.preventDefault();
                if (isDesktop()) {
                    toggleDesktopSidebar();
                } else {
                    toggleMobileSidebar();
                }
            });
        }

        // Mobile backdrop click
        if (sidebarBackdrop) {
            sidebarBackdrop.addEventListener("click", closeMobileSidebar);
        }

        // Nav links click closes mobile drawer
        document.querySelectorAll(".sidebar-link").forEach(function (link) {
            link.addEventListener("click", function () {
                if (!isDesktop()) {
                    closeMobileSidebar();
                }
            });
        });

        // Handle viewport resize across mobile/desktop boundary
        let lastWidth = window.innerWidth;
        window.addEventListener("resize", function () {
            const currentWidth = window.innerWidth;
            if (lastWidth <= MOBILE_BREAKPOINT && currentWidth > MOBILE_BREAKPOINT) {
                // Transitioned to desktop
                closeMobileSidebar();
                applyDesktopState(getStoredCollapseState(), false);
            } else if (lastWidth > MOBILE_BREAKPOINT && currentWidth <= MOBILE_BREAKPOINT) {
                // Transitioned to mobile
                document.documentElement.classList.remove("sidebar-collapsed");
            }
            lastWidth = currentWidth;
        });


        // =====================================================
        // AUTO-DISMISS TOASTS
        // =====================================================
        const toasts = document.querySelectorAll(".dss-toast");
        toasts.forEach(function (toast) {
            setTimeout(function () {
                toast.style.opacity = "0";
                toast.style.transform = "translateX(20px)";
                setTimeout(function () {
                    if (toast.parentElement) {
                        toast.remove();
                    }
                }, 300);
            }, 5000);
        });

        // =====================================================
        // CONFIRMATION MODAL SUBMIT BUTTON & INTERACTIONS
        // =====================================================
        const confirmSubmitBtn = document.getElementById("dssConfirmSubmitBtn");
        function resetModalProcessingState() {
            const submitTextEl = document.getElementById("dssConfirmSubmitText");
            const submitSpinnerEl = document.getElementById("dssConfirmSubmitSpinner");
            const cancelBtn = document.getElementById("dssConfirmCancelBtn");
            const closeBtn = document.getElementById("dssConfirmCloseBtn");

            DSS._isProcessing = false;
            if (confirmSubmitBtn) confirmSubmitBtn.disabled = false;
            if (cancelBtn) cancelBtn.disabled = false;
            if (closeBtn) closeBtn.disabled = false;
            if (submitSpinnerEl) submitSpinnerEl.style.display = "none";
            if (submitTextEl) submitTextEl.textContent = DSS._currentConfirmText || "Confirm";
        }

        if (confirmSubmitBtn) {
            confirmSubmitBtn.addEventListener("click", function () {
                if (DSS._isProcessing) return; // Prevent double submit
                DSS._isProcessing = true;

                // Enter loading state
                const submitTextEl = document.getElementById("dssConfirmSubmitText");
                const submitSpinnerEl = document.getElementById("dssConfirmSubmitSpinner");
                const cancelBtn = document.getElementById("dssConfirmCancelBtn");
                const closeBtn = document.getElementById("dssConfirmCloseBtn");

                if (submitSpinnerEl) submitSpinnerEl.style.display = "inline-block";
                if (submitTextEl) {
                    submitTextEl.textContent = "Processing...";
                }
                confirmSubmitBtn.disabled = true;
                if (cancelBtn) cancelBtn.disabled = true;
                if (closeBtn) closeBtn.disabled = true;

                const inputEl = document.getElementById("dssConfirmInput");
                const inputValue = inputEl ? inputEl.value.trim() : "";
                const callback = DSS._confirmCallback;

                if (typeof callback === "function") {
                    try {
                        Promise.resolve(callback(inputValue)).catch(function (err) {
                            console.error("Confirmation async callback error:", err);
                            resetModalProcessingState();
                        });
                    } catch (err) {
                        console.error("Confirmation callback error:", err);
                        resetModalProcessingState();
                    }
                } else {
                    resetModalProcessingState();
                }
            });
        }

        // Close modal or mobile sidebar on Escape, and trap focus inside modal when open
        document.addEventListener("keydown", function (e) {
            const modal = document.getElementById("dssConfirmModal");
            const isModalOpen = modal && modal.getAttribute("aria-hidden") === "false";

            if (e.key === "Escape") {
                if (isModalOpen && !DSS._isProcessing) {
                    DSS.closeConfirmModal();
                } else if (document.body.classList.contains("sidebar-mobile-open")) {
                    closeMobileSidebar();
                    if (sidebarToggleBtn && typeof sidebarToggleBtn.focus === "function") {
                        sidebarToggleBtn.focus();
                    }
                }
                return;
            }

            if (e.key === "Tab" && isModalOpen) {
                const focusable = Array.from(
                    modal.querySelectorAll("button:not(:disabled), textarea:not(:disabled), input:not(:disabled), [tabindex]:not([tabindex='-1'])")
                ).filter(function (el) {
                    return el.offsetParent !== null;
                });

                if (focusable.length > 0) {
                    const first = focusable[0];
                    const last = focusable[focusable.length - 1];
                    if (e.shiftKey && document.activeElement === first) {
                        e.preventDefault();
                        last.focus();
                    } else if (!e.shiftKey && document.activeElement === last) {
                        e.preventDefault();
                        first.focus();
                    }
                }
            }
        });

        // =====================================================
        // DATA-DSS-CONFIRM DECLARATIVE ATTR
        // =====================================================
        document.addEventListener("click", function (e) {
            const trigger = e.target.closest("[data-dss-confirm]");
            if (!trigger) return;

            const form = trigger.closest("form") || trigger.form || (trigger.getAttribute("form") ? document.getElementById(trigger.getAttribute("form")) : null);

            // Validate form BEFORE opening confirmation modal so invalid forms are not locked
            if (form && !trigger.hasAttribute("formnovalidate")) {
                if (typeof form.checkValidity === "function" && !form.checkValidity()) {
                    e.preventDefault();
                    if (typeof form.reportValidity === "function") {
                        form.reportValidity();
                    }
                    return;
                }
            }

            e.preventDefault();
            const message = trigger.getAttribute("data-dss-confirm");
            const title = trigger.getAttribute("data-dss-title") || "Confirm Action";
            const confirmText = trigger.getAttribute("data-dss-btn-text") || "Confirm";
            const consequence = trigger.getAttribute("data-dss-consequence");

            let actionType = trigger.getAttribute("data-dss-type");
            if (!actionType) {
                if (trigger.classList.contains("btn-danger") ||
                    trigger.classList.contains("text-danger") ||
                    trigger.classList.contains("border-danger") ||
                    trigger.classList.contains("btn-outline-danger")) {
                    actionType = "danger";
                } else if (trigger.classList.contains("btn-warning") ||
                           trigger.classList.contains("text-warning") ||
                           trigger.classList.contains("border-warning") ||
                           trigger.classList.contains("btn-outline-warning")) {
                    actionType = "warning";
                } else if (trigger.classList.contains("btn-success") ||
                           trigger.classList.contains("text-success") ||
                           trigger.classList.contains("border-success") ||
                           trigger.classList.contains("btn-outline-success")) {
                    actionType = "success";
                } else {
                    actionType = "primary";
                }
            }

            DSS.showConfirmModal({
                title: title,
                message: message,
                consequence: consequence,
                confirmText: confirmText,
                actionType: actionType,
                onConfirm: function () {
                    if (form) {
                        // Submit the form via requestSubmit when supported so submit listeners & validation run
                        if (trigger.type === "submit" && trigger.name) {
                            let existingHidden = form.querySelector(`input[type="hidden"][data-dss-submit-mirror="${trigger.name}"]`);
                            if (!existingHidden) {
                                existingHidden = document.createElement("input");
                                existingHidden.type = "hidden";
                                existingHidden.setAttribute("data-dss-submit-mirror", trigger.name);
                                existingHidden.name = trigger.name;
                                form.appendChild(existingHidden);
                            }
                            existingHidden.value = trigger.value;
                        }
                        if (typeof form.requestSubmit === "function") {
                            form.requestSubmit();
                        } else {
                            form.submit();
                        }
                    } else if (trigger.tagName === "A" && trigger.href) {
                        window.location.href = trigger.href;
                    }
                }
            });
        });

        // =====================================================
        // DOUBLE SUBMIT PREVENTION & LOADING STATE
        // =====================================================
        document.addEventListener("submit", function (e) {
            const form = e.target;
            if (form.getAttribute("data-no-disable") !== null || form.target === "_blank") {
                return;
            }

            // If HTML5 form validation fails, do not disable
            if (typeof form.checkValidity === "function" && !form.checkValidity()) {
                return;
            }

            let submitButtons = Array.from(form.querySelectorAll("button[type='submit'], input[type='submit']"));
            if (form.id) {
                const externalButtons = document.querySelectorAll(`button[form='${form.id}'], input[form='${form.id}']`);
                submitButtons = submitButtons.concat(Array.from(externalButtons));
            }

            submitButtons.forEach(function (btn) {
                if (btn.classList.contains("btn-loading")) {
                    e.preventDefault();
                    return;
                }

                // Apply loading state
                btn.classList.add("btn-loading");
                if (btn.tagName === "BUTTON") {
                    const spinner = document.createElement("span");
                    spinner.className = "spinner-border-sm";
                    btn.prepend(spinner);
                }
            });
        });

        // =====================================================
        // SUPPLIER RADIO SELECTION STATE
        // =====================================================
        document.addEventListener("change", function (e) {
            if (e.target && e.target.classList.contains("decision-supplier-radio")) {
                const container = e.target.closest(".decision-supplier-selection");
                if (container) {
                    container.querySelectorAll(".decision-supplier-option").forEach(function (opt) {
                        opt.classList.remove("is-selected");
                    });
                }
                const parentLabel = e.target.closest(".decision-supplier-option");
                if (parentLabel) {
                    parentLabel.classList.add("is-selected");
                }
            }
        });

    });

})();