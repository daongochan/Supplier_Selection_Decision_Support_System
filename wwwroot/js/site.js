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

        showConfirmModal: function (options) {
            const modal = document.getElementById("dssConfirmModal");
            if (!modal) {
                if (confirm(options.message || "Bạn có chắc chắn muốn thực hiện thao tác này?")) {
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
            if (titleEl) titleEl.textContent = options.title || "Xác nhận thao tác";
            if (messageEl) messageEl.textContent = options.message || "Bạn có chắc chắn muốn tiếp tục?";
            if (submitTextEl) {
                submitTextEl.textContent = options.confirmText || "Xác nhận";
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
                if (inputLabel) inputLabel.textContent = options.inputLabel || "Ghi chú / Lý do:";
                if (inputEl) {
                    inputEl.value = "";
                    inputEl.placeholder = options.inputPlaceholder || "Nhập lý do hoặc ý kiến...";
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
        // SIDEBAR STATE CLEANUP (REVERT TO STATIC SIDEBAR)
        // =====================================================
        try {
            localStorage.removeItem("dss_sidebar_collapsed");
        } catch (e) { }
        document.documentElement.classList.remove("sidebar-collapsed");
        document.body.classList.remove("sidebar-collapsed", "sidebar-mobile-open");


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
                    submitTextEl.textContent = "Đang xử lý...";
                }
                confirmSubmitBtn.disabled = true;
                if (cancelBtn) cancelBtn.disabled = true;
                if (closeBtn) closeBtn.disabled = true;

                const inputEl = document.getElementById("dssConfirmInput");
                const inputValue = inputEl ? inputEl.value.trim() : "";
                const callback = DSS._confirmCallback;

                if (typeof callback === "function") {
                    try {
                        callback(inputValue);
                    } catch (err) {
                        console.error("Confirmation callback error:", err);
                        DSS._isProcessing = false;
                        confirmSubmitBtn.disabled = false;
                        if (cancelBtn) cancelBtn.disabled = false;
                        if (closeBtn) closeBtn.disabled = false;
                        if (submitSpinnerEl) submitSpinnerEl.style.display = "none";
                        if (submitTextEl) submitTextEl.textContent = "Xác nhận";
                    }
                }
            });
        }

        // Close modal on Escape (only when idle)
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") {
                if (!DSS._isProcessing) {
                    DSS.closeConfirmModal();
                }
            }
        });

        // =====================================================
        // DATA-DSS-CONFIRM DECLARATIVE ATTR
        // =====================================================
        document.addEventListener("click", function (e) {
            const trigger = e.target.closest("[data-dss-confirm]");
            if (!trigger) return;

            e.preventDefault();
            const message = trigger.getAttribute("data-dss-confirm");
            const title = trigger.getAttribute("data-dss-title") || "Xác nhận";
            const confirmText = trigger.getAttribute("data-dss-btn-text") || "Đồng ý";
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

            const form = trigger.closest("form") || trigger.form || (trigger.getAttribute("form") ? document.getElementById(trigger.getAttribute("form")) : null);

            DSS.showConfirmModal({
                title: title,
                message: message,
                consequence: consequence,
                confirmText: confirmText,
                actionType: actionType,
                onConfirm: function () {
                    if (form) {
                        // Submit the form
                        if (trigger.type === "submit" && trigger.name) {
                            const hidden = document.createElement("input");
                            hidden.type = "hidden";
                            hidden.name = trigger.name;
                            hidden.value = trigger.value;
                            form.appendChild(hidden);
                        }
                        form.submit();
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