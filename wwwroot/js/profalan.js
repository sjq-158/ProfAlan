window.profAlan = {
    palette: {
        get: function () {
            return document.documentElement.getAttribute("data-palette") || "platinum";
        },
        set: function (name) {
            document.documentElement.setAttribute("data-palette", name);
            try { localStorage.setItem("profalan.palette", name); } catch (e) { }
        }
    },

    storage: {
        area: function (session) { return session ? window.sessionStorage : window.localStorage; },
        get: function (key, session) {
            try { return this.area(session).getItem(key); } catch (e) { return null; }
        },
        set: function (key, value, session) {
            try { this.area(session).setItem(key, value); } catch (e) { }
        },
        remove: function (key, session) {
            try { this.area(session).removeItem(key); } catch (e) { }
        }
    },

    // Converts a pointer position into the SVG's own coordinate system so dragging works at any zoom.
    svgPoint: function (svg, clientX, clientY) {
        if (!svg || !svg.getScreenCTM) return { x: 0, y: 0 };
        var pt = svg.createSVGPoint();
        pt.x = clientX;
        pt.y = clientY;
        var p = pt.matrixTransform(svg.getScreenCTM().inverse());
        return { x: p.x, y: p.y };
    },

    // Wheel zoom needs a non-passive listener so the page does not scroll underneath the canvas.
    attachWheelZoom: function (element, dotnet) {
        if (!element || element.__profAlanWheel) return;
        var handler = function (e) {
            e.preventDefault();
            dotnet.invokeMethodAsync("OnWheelZoom", e.deltaY, e.clientX, e.clientY);
        };
        element.addEventListener("wheel", handler, { passive: false });
        element.__profAlanWheel = handler;
    },

    detachWheelZoom: function (element) {
        if (element && element.__profAlanWheel) {
            element.removeEventListener("wheel", element.__profAlanWheel);
            delete element.__profAlanWheel;
        }
    },

    svgScale: function (svg) {
        var ctm = svg && svg.getScreenCTM ? svg.getScreenCTM() : null;
        return ctm ? ctm.a : 1;
    },

    scrollIntoView: function (element) {
        if (element) element.scrollIntoView({ block: "nearest", behavior: "smooth" });
    },

    scrollToOffset: function (element, top) {
        if (element) element.scrollTo({ top: Math.max(0, top), behavior: "smooth" });
    },

    focus: function (element) {
        if (element) element.focus();
    },

    copyText: function (text) {
        return navigator.clipboard ? navigator.clipboard.writeText(text).then(function () { return true; }, function () { return false; }) : false;
    },

    downloadText: function (filename, text) {
        var blob = new Blob([text], { type: "application/json" });
        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
    }
};
