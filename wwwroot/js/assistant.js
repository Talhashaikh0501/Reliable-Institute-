document.addEventListener("DOMContentLoaded", function () {
    const chatRoot = document.getElementById("ra-assistant");

    if (chatRoot) {
        const messagesHost = document.getElementById("assistant-messages");
        const welcome = messagesHost.querySelector("[data-welcome]");
        const form = document.getElementById("assistant-form");
        const input = document.getElementById("assistant-input");
        const sendButton = document.getElementById("assistant-send");
        const tokenInput = chatRoot.querySelector('input[name="__RequestVerificationToken"]');
        const storageKey = "reliable-assistant-chat-v1-" + (chatRoot.dataset.userScope || "admin");
        let messages = [];
        let isBusy = false;
        const assistantReady = chatRoot.dataset.isReady === "true";

        function appendInlineFormatting(parent, text) {
            const pattern = /(\*\*[^*]+\*\*|\*[^*\n]+\*)/g;
            let lastIndex = 0;
            let match;

            while ((match = pattern.exec(text)) !== null) {
                if (match.index > lastIndex) {
                    parent.appendChild(document.createTextNode(text.slice(lastIndex, match.index)));
                }

                const token = match[0];
                const element = document.createElement(token.startsWith("**") ? "strong" : "em");
                element.textContent = token.startsWith("**")
                    ? token.slice(2, -2)
                    : token.slice(1, -1);
                parent.appendChild(element);
                lastIndex = pattern.lastIndex;
            }

            if (lastIndex < text.length) {
                parent.appendChild(document.createTextNode(text.slice(lastIndex)));
            }
        }

        function renderAssistantMarkdown(container, markdown) {
            const lines = String(markdown || "").replace(/\r\n?/g, "\n").split("\n");
            let index = 0;

            function isTableSeparator(line) {
                return /^\s*\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?\s*$/.test(line);
            }

            function tableCells(line) {
                return line.trim().replace(/^\|/, "").replace(/\|$/, "")
                    .split("|").map(cell => cell.trim());
            }

            function startsBlock(line, nextLine) {
                return /^#{1,3}\s+/.test(line) ||
                    /^[-*_]{3,}\s*$/.test(line) ||
                    /^>\s?/.test(line) ||
                    /^\s*(?:[-+*]|\d+[.)])\s+/.test(line) ||
                    (line.includes("|") && nextLine !== undefined && isTableSeparator(nextLine));
            }

            while (index < lines.length) {
                const line = lines[index];
                if (!line.trim()) {
                    index++;
                    continue;
                }

                const heading = line.match(/^(#{1,3})\s+(.+)$/);
                if (heading) {
                    const level = heading[1].length;
                    const element = document.createElement(level === 1 ? "h2" : level === 2 ? "h3" : "h4");
                    appendInlineFormatting(element, heading[2].trim());
                    container.appendChild(element);
                    index++;
                    continue;
                }

                if (/^[-*_]{3,}\s*$/.test(line)) {
                    container.appendChild(document.createElement("hr"));
                    index++;
                    continue;
                }

                if (line.includes("|") && index + 1 < lines.length && isTableSeparator(lines[index + 1])) {
                    const wrapper = document.createElement("div");
                    wrapper.className = "ra-assistant-table-wrap";
                    const table = document.createElement("table");
                    const head = document.createElement("thead");
                    const headerRow = document.createElement("tr");

                    tableCells(line).forEach(cell => {
                        const th = document.createElement("th");
                        appendInlineFormatting(th, cell);
                        headerRow.appendChild(th);
                    });

                    head.appendChild(headerRow);
                    table.appendChild(head);
                    const body = document.createElement("tbody");
                    index += 2;

                    while (index < lines.length && lines[index].trim() && lines[index].includes("|")) {
                        const row = document.createElement("tr");
                        tableCells(lines[index]).forEach(cell => {
                            const td = document.createElement("td");
                            appendInlineFormatting(td, cell);
                            row.appendChild(td);
                        });
                        body.appendChild(row);
                        index++;
                    }

                    table.appendChild(body);
                    wrapper.appendChild(table);
                    container.appendChild(wrapper);
                    continue;
                }

                if (/^>\s?/.test(line)) {
                    const quote = document.createElement("blockquote");
                    while (index < lines.length && /^>\s?/.test(lines[index])) {
                        const paragraph = document.createElement("p");
                        appendInlineFormatting(paragraph, lines[index].replace(/^>\s?/, "").trim());
                        quote.appendChild(paragraph);
                        index++;
                    }
                    container.appendChild(quote);
                    continue;
                }

                const listMatch = line.match(/^\s*([-+*]|\d+[.)])\s+(.+)$/);
                if (listMatch) {
                    const ordered = /^\d/.test(listMatch[1]);
                    const list = document.createElement(ordered ? "ol" : "ul");
                    while (index < lines.length) {
                        const itemMatch = lines[index].match(/^\s*([-+*]|\d+[.)])\s+(.+)$/);
                        if (!itemMatch || /^\d/.test(itemMatch[1]) !== ordered) break;
                        const item = document.createElement("li");
                        appendInlineFormatting(item, itemMatch[2].trim());
                        list.appendChild(item);
                        index++;
                    }
                    container.appendChild(list);
                    continue;
                }

                const paragraphLines = [];
                while (index < lines.length && lines[index].trim() &&
                    !(paragraphLines.length > 0 && startsBlock(lines[index], lines[index + 1]))) {
                    paragraphLines.push(lines[index].trim());
                    index++;
                }

                const paragraph = document.createElement("p");
                appendInlineFormatting(paragraph, paragraphLines.join(" "));
                container.appendChild(paragraph);
            }
        }

        try {
            const saved = JSON.parse(sessionStorage.getItem(storageKey) || "[]");
            if (Array.isArray(saved)) {
                messages = saved.filter(item =>
                    item && (item.role === "user" || item.role === "assistant") &&
                    typeof item.content === "string").slice(-24);
            }
        } catch (_) {
            messages = [];
        }

        function saveMessages() {
            try {
                sessionStorage.setItem(storageKey, JSON.stringify(messages.slice(-24)));
            } catch (_) {
                // The current conversation remains usable even when browser storage is unavailable.
            }
        }

        function updateWelcome() {
            welcome.hidden = messages.length > 0;
        }

        function addMessage(role, content, isError) {
            const article = document.createElement("article");
            article.className = "ra-assistant-message" +
                (role === "user" ? " is-user" : "") +
                (isError ? " is-error" : "");

            const avatar = document.createElement("span");
            avatar.className = "ra-assistant-message-avatar";
            avatar.textContent = role === "user" ? "You" : "R.";

            const contentWrap = document.createElement("div");
            contentWrap.className = "ra-assistant-message-content";

            const label = document.createElement("span");
            label.className = "ra-assistant-message-label";
            label.textContent = role === "user" ? "You" : "Reliable Assistant";

            const body = document.createElement("div");
            body.className = "ra-assistant-message-body" +
                (role === "assistant" && !isError ? " is-formatted" : "");
            if (role === "assistant" && !isError) {
                renderAssistantMarkdown(body, content);
            } else {
                body.textContent = content;
            }

            contentWrap.append(label, body);
            article.append(avatar, contentWrap);
            messagesHost.appendChild(article);
            messagesHost.scrollTop = messagesHost.scrollHeight;
            updateWelcome();
        }

        function renderHistory() {
            messagesHost.querySelectorAll(".ra-assistant-message").forEach(node => node.remove());
            messages.forEach(item => addMessage(item.role, item.content, false));
            updateWelcome();
        }

        function setBusy(value) {
            isBusy = value;
            sendButton.disabled = value || !assistantReady || input.value.trim().length === 0;
            input.disabled = value || !assistantReady;
            sendButton.querySelector("span").textContent = value ? "Thinking..." : "Send question";
            messagesHost.querySelector("[data-typing]")?.remove();

            if (value) {
                const typing = document.createElement("div");
                typing.className = "ra-assistant-message";
                typing.dataset.typing = "true";
                typing.innerHTML = '<span class="ra-assistant-message-avatar">R.</span><div><span class="ra-assistant-message-label">Reliable Assistant</span><div class="ra-assistant-typing"><span></span><span></span><span></span></div></div>';
                messagesHost.appendChild(typing);
                messagesHost.scrollTop = messagesHost.scrollHeight;
            }
        }

        async function sendMessage(text) {
            if (isBusy || !text.trim()) return;
            const question = text.trim().slice(0, 2000);
            messages.push({ role: "user", content: question });
            addMessage("user", question, false);
            saveMessages();
            input.value = "";
            input.style.height = "auto";
            setBusy(true);

            try {
                const response = await fetch(chatRoot.dataset.chatUrl, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "RequestVerificationToken": tokenInput ? tokenInput.value : ""
                    },
                    body: JSON.stringify({
                        message: question,
                        history: messages.slice(0, -1).slice(-12)
                    })
                });

                const payload = await response.json().catch(() => ({}));
                if (!response.ok || !payload.success) {
                    throw new Error(payload.message || "The request could not be completed. Please try again.");
                }

                const answer = typeof payload.answer === "string" ? payload.answer : "No answer was returned.";
                messages.push({ role: "assistant", content: answer });
                addMessage("assistant", answer, false);
                saveMessages();
            } catch (error) {
                const message = error && error.message
                    ? error.message
                    : "A connection problem occurred. Please try again.";
                addMessage("assistant", message, true);
            } finally {
                setBusy(false);
                input.focus();
            }
        }

        form.addEventListener("submit", function (event) {
            event.preventDefault();
            sendMessage(input.value);
        });

        input.addEventListener("input", function () {
            input.style.height = "auto";
            input.style.height = Math.min(input.scrollHeight, 150) + "px";
            sendButton.disabled = isBusy || !assistantReady || input.value.trim().length === 0;
        });

        input.addEventListener("keydown", function (event) {
            if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                form.requestSubmit();
            }
        });

        chatRoot.querySelectorAll("[data-prompt]").forEach(button => {
            button.addEventListener("click", function () {
                sendMessage(button.getAttribute("data-prompt") || "");
            });
        });

        chatRoot.querySelector("[data-new-chat]")?.addEventListener("click", function () {
            messages = [];
            saveMessages();
            messagesHost.querySelectorAll(".ra-assistant-message").forEach(node => node.remove());
            updateWelcome();
            input.value = "";
            input.focus();
            sendButton.disabled = true;
        });

        chatRoot.querySelector("[data-clear-chat]")?.addEventListener("click", function () {
            messages = [];
            saveMessages();
            messagesHost.querySelectorAll(".ra-assistant-message").forEach(node => node.remove());
            updateWelcome();
        });

        renderHistory();
    }

    const settingsForm = document.getElementById("assistant-settings-form");
    if (settingsForm) {
        const keyInput = document.getElementById("assistant-api-key");
        const toggleKey = settingsForm.querySelector("[data-toggle-key]");
        const testButton = document.getElementById("assistant-test-button");
        const testResult = document.getElementById("assistant-test-result");

        toggleKey?.addEventListener("click", function () {
            const show = keyInput.type === "password";
            keyInput.type = show ? "text" : "password";
            toggleKey.textContent = show ? "Hide" : "Show";
            toggleKey.setAttribute("aria-label", show ? "Hide API key" : "Show API key");
        });

        testButton?.addEventListener("click", async function () {
            const token = settingsForm.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
            const body = new URLSearchParams();
            body.set("apiKey", keyInput.value);
            testButton.disabled = true;
            testButton.textContent = "Testing connection...";
            testResult.hidden = false;
            testResult.classList.remove("is-error");
            testResult.textContent = "Checking the connection securely...";

            try {
                const response = await fetch(settingsForm.dataset.testUrl, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/x-www-form-urlencoded",
                        "RequestVerificationToken": token
                    },
                    body: body.toString()
                });
                const result = await response.json().catch(() => ({}));
                testResult.textContent = result.message || "The connection test could not be completed.";
                if (!response.ok || !result.success) testResult.classList.add("is-error");
            } catch (_) {
                testResult.textContent = "The connection test could not reach the server. Check your connection and try again.";
                testResult.classList.add("is-error");
            } finally {
                testButton.disabled = false;
                testButton.textContent = "Test connection";
            }
        });
    }
});
