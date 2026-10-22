/**
 * SupplierSelectionDSS - Enterprise Client Script
 */

(function () {
    "use strict";

    // =========================================================
    // GLOBAL DSS UTILITIES & MODAL
    // =========================================================
    window.DSS = {
        _confirmCallback: null,

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

            const titleEl = document.getElementById("dssConfirmTitle");
            const messageEl = document.getElementById("dssConfirmMessage");
            const iconEl = document.getElementById("dssConfirmIcon");
            const submitBtn = document.getElementById("dssConfirmSubmitBtn");
            const inputGroup = document.getElementById("dssConfirmInputGroup");
            const inputLabel = document.getElementById("dssConfirmInputLabel");
            const inputEl = document.getElementById("dssConfirmInput");

            if (titleEl) titleEl.textContent = options.title || "Xác nhận thao tác";
            if (messageEl) messageEl.textContent = options.message || "Bạn có chắc chắn muốn tiếp tục?";
            if (iconEl) {
                iconEl.className = options.iconClass || "bi bi-question-circle-fill text-warning";
            }
            if (submitBtn) {
                submitBtn.textContent = options.confirmText || "Xác nhận";
                submitBtn.className = options.confirmBtnClass || "btn-primary";
            }

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
            if (options.requireInput && inputEl) {
                setTimeout(function () { inputEl.focus(); }, 100);
            }
        },

        closeConfirmModal: function () {
            const modal = document.getElementById("dssConfirmModal");
            if (modal) {
                modal.style.display = "none";
                modal.setAttribute("aria-hidden", "true");
            }
            this._confirmCallback = null;
        }
    };

    document.addEventListener("DOMContentLoaded", function () {

        // =====================================================
        // SIDEBAR TOGGLE & RESPONSIVE
        // =====================================================
        const sidebar = document.querySelector(".app-sidebar");
        const sidebarToggle = document.querySelector("#sidebarToggle");

        if (sidebar && sidebarToggle) {
            sidebarToggle.addEventListener("click", function () {
                sidebar.classList.toggle("open");
                const isOpen = sidebar.classList.contains("open");
                sidebarToggle.setAttribute("aria-expanded", isOpen.toString());
            });

            const sidebarLinks = sidebar.querySelectorAll(".sidebar-link");
            sidebarLinks.forEach(function (link) {
                link.addEventListener("click", function () {
                    if (window.innerWidth <= 900) {
                        sidebar.classList.remove("open");
                        sidebarToggle.setAttribute("aria-expanded", "false");
                    }
                });
            });
        }

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
        // CONFIRMATION MODAL SUBMIT BUTTON
        // =====================================================
        const confirmSubmitBtn = document.getElementById("dssConfirmSubmitBtn");
        if (confirmSubmitBtn) {
            confirmSubmitBtn.addEventListener("click", function () {
                const inputEl = document.getElementById("dssConfirmInput");
                const inputValue = inputEl ? inputEl.value.trim() : "";
                const callback = DSS._confirmCallback;
                DSS.closeConfirmModal();
                if (typeof callback === "function") {
                    callback(inputValue);
                }
            });
        }

        // Close modal on Escape
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") {
                DSS.closeConfirmModal();
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
            const isDanger = trigger.classList.contains("btn-danger") || trigger.classList.contains("text-danger") || trigger.classList.contains("btn-outline-danger");
            const form = trigger.closest("form") || trigger.form || (trigger.getAttribute("form") ? document.getElementById(trigger.getAttribute("form")) : null);

            DSS.showConfirmModal({
                title: title,
                message: message,
                confirmText: confirmText,
                confirmBtnClass: isDanger ? "btn-primary btn-danger" : "btn-primary",
                iconClass: isDanger ? "bi bi-exclamation-triangle-fill text-danger" : "bi bi-question-circle-fill text-warning",
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