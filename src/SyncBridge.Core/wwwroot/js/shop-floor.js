"use strict";
(() => {
    const form = document.getElementById("order-lookup");
    const input = document.getElementById("order-number");
    const button = form.querySelector("button");
    const feedback = document.getElementById("lookup-feedback");
    const context = document.getElementById("manufacturing-context");
    const results = document.getElementById("order-context");
    const mes = document.getElementById("mes-status");
    const fields = ["orderNumber", "productCode", "productName", "recipeId", "quantity", "status"];
    const statuses = ["Planned", "Released", "InProcess", "Completed", "Cancelled"];
    const errors = {
        400: "Invalid Production Order Number. Enter or scan 1–40 characters.",
        401: "Your session has expired. Sign in again, then return to this terminal.",
        403: "Your role does not permit this retrieval.",
        404: "Production Order not found. Check the number and try again."
    };
    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (button.disabled) return;
        results.hidden = true;
        results.querySelectorAll("[data-field]").forEach(cell => { cell.textContent = ""; });
        mes.textContent = "MES ready";
        mes.classList.remove("linked");
        feedback.classList.remove("error");
        const number = input.value;
        // Preserve scanner text exactly. The API accepts nonblank text up to 40 characters.
        if (!number.trim() || number.length > 40 || /[\u0000-\u001f\u007f]/u.test(number)) {
            feedback.textContent = errors[400];
            feedback.classList.add("error");
            input.focus();
            return;
        }
        button.disabled = true;
        context.setAttribute("aria-busy", "true");
        feedback.textContent = "Retrieving Production Order…";
        try {
            const response = await fetch(form.dataset.apiUrl + encodeURIComponent(number), {
                method: "GET", credentials: "same-origin", cache: "no-store", redirect: "error",
                headers: { Accept: "application/json" }, signal: AbortSignal.timeout(15000)
            });
            if (!response.ok) throw new Error(errors[response.status] || "Retrieval unsuccessful. Try again later.");
            const order = await response.json();
            if (!order || !fields.every(field => Object.hasOwn(order, field)) ||
                !fields.slice(0, 4).every(field => typeof order[field] === "string") ||
                typeof order.quantity !== "number" || !Number.isInteger(order.status) || !statuses[order.status]) {
                throw new Error("Unexpected response. Try again later.");
            }
            results.querySelectorAll("[data-field]").forEach(cell => {
                const field = cell.dataset.field;
                cell.textContent = field === "status" ? statuses[order.status] : String(order[field]);
            });
            results.hidden = false;
            mes.textContent = "MES Linked";
            mes.classList.add("linked");
            feedback.textContent = "Production Order retrieved.";
        } catch (error) {
            feedback.textContent = error instanceof Error &&
                [...Object.values(errors), "Retrieval unsuccessful. Try again later.", "Unexpected response. Try again later."].includes(error.message)
                ? error.message : "Unable to reach MES services. Check the connection and try again.";
            feedback.classList.add("error");
            mes.textContent = "Retrieval failed";
        } finally {
            button.disabled = false;
            context.setAttribute("aria-busy", "false");
            input.focus();
            input.select();
        }
    });
})();
