window.voucherApp = {

    showToast: function (message, type) {
        const tone = type === 'error'
            ? { bg: '#dc2626', shadow: 'rgba(220,38,38,.35)' }
            : { bg: '#4f46e5', shadow: 'rgba(79,70,229,.35)' };

        const toast = document.createElement('div');
        toast.textContent = message;
        toast.style.cssText = [
            'position:fixed', 'bottom:1.5rem', 'right:1.5rem',
            `background:${tone.bg}`, 'color:#fff',
            'padding:.45rem 1rem', 'border-radius:8px',
            'font-size:.875rem', 'font-family:sans-serif',
            `box-shadow:0 4px 12px ${tone.shadow}`,
            'z-index:99999', 'opacity:1',
            'transition:opacity .35s ease'
        ].join(';');

        document.body.appendChild(toast);
        setTimeout(() => {
            toast.style.opacity = '0';
            setTimeout(() => toast.remove(), 350);
        }, 1500);
    },

    print: function () {
        window.print();
    },

    copyText: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
        } catch {
            // Fallback for HTTP
            const ta = document.createElement('textarea');
            ta.value = text;
            ta.style.cssText = 'position:fixed;top:-9999px;left:-9999px';
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
        }
        window.voucherApp.showToast('Copied!', 'success');
    },

    downloadPdf: async function () {
        const area = document.getElementById('printArea');
        if (!area) {
            alert('Nothing to download. Generate vouchers first.');
            return;
        }

        const exportCard = area.querySelector('.print-preview-card-export');
        if (!exportCard) {
            alert('Export layout not found.');
            return;
        }

        const savedStyle = area.getAttribute('style') || '';

        // Bring on-screen so layout is fully computed
        area.style.cssText = 'position:fixed;left:0;top:0;z-index:-9999;width:1060px;';

        // Wait two frames for layout to settle
        await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));

        const areaRect = area.getBoundingClientRect();
        const exportCardRect = exportCard.getBoundingClientRect();
        const exportBottomDom = exportCardRect.bottom - areaRect.top;

        // Collect the bottom Y of every complete card row (4 cards per row)
        // These are the only safe page-break positions.
        const cards = Array.from(area.querySelectorAll('.preview-voucher-card'));
        const safeBreaksDom = []; // DOM px relative to area top
        for (let i = 0; i < cards.length; i++) {
            if ((i + 1) % 4 === 0 || i === cards.length - 1) {
                const rect = cards[i].getBoundingClientRect();
                safeBreaksDom.push(rect.bottom - areaRect.top);
            }
        }

        const exportHeightDom = exportBottomDom;

        try {
            const canvas = await html2canvas(area, {
                scale: 2,
                useCORS: true,
                backgroundColor: '#ffffff',
                width: area.scrollWidth,
                height: Math.ceil(exportHeightDom),
                windowWidth: 1100
            });

            const { jsPDF } = window.jspdf;
            const pdf        = new jsPDF({ orientation: 'portrait', unit: 'pt', format: 'a4' });
            const pageWidth  = pdf.internal.pageSize.getWidth();
            const pageHeight = pdf.internal.pageSize.getHeight();
            const margin     = 20;
            const printWidth = pageWidth - margin * 2;

            // Ratios between coordinate spaces
            const pxPerDomPx = canvas.width / areaRect.width;   // canvas px / DOM px
            const ptPerDomPx = printWidth   / areaRect.width;   // PDF pt   / DOM px
            const maxPageDom = (pageHeight - margin * 2) / ptPerDomPx; // max DOM px per page

            // Build page slices, snapping end points to safe row breaks
            const pages = [];
            let startDom = 0;
            const minSliceDom = 6;
            const trailingDecorationDom = 32;

            while (startDom < exportHeightDom - 1) {
                const idealEnd = startDom + maxPageDom;

                // Find the last safe break that is > startDom and <= idealEnd
                let endDom = null;
                for (let j = safeBreaksDom.length - 1; j >= 0; j--) {
                    if (safeBreaksDom[j] > startDom && safeBreaksDom[j] <= idealEnd) {
                        endDom = safeBreaksDom[j];
                        break;
                    }
                }

                // If no row fits within the page, force-break at idealEnd (edge case)
                if (endDom === null || endDom <= startDom) {
                    endDom = Math.min(idealEnd, exportHeightDom);
                }

                if ((exportHeightDom - endDom) < minSliceDom) {
                    endDom = exportHeightDom;
                }

                if ((endDom - startDom) < minSliceDom) {
                    break;
                }

                pages.push({ startDom, endDom });
                startDom = endDom;
            }

            if (pages.length > 1) {
                const lastPage = pages[pages.length - 1];
                const previousPage = pages[pages.length - 2];
                const trailingHeightDom = lastPage.endDom - lastPage.startDom;

                if (trailingHeightDom <= trailingDecorationDom) {
                    previousPage.endDom = lastPage.endDom;
                    pages.pop();
                }
            }

            // Render each page slice from the canvas
            for (let p = 0; p < pages.length; p++) {
                const { startDom, endDom } = pages[p];
                const startPx  = Math.round(startDom  * pxPerDomPx);
                const heightPx = Math.round((endDom - startDom) * pxPerDomPx);
                if (heightPx <= 0) continue;

                const slice = document.createElement('canvas');
                slice.width  = canvas.width;
                slice.height = heightPx;
                slice.getContext('2d').drawImage(
                    canvas, 0, startPx, canvas.width, heightPx,
                    0, 0, canvas.width, heightPx
                );

                if (p > 0) pdf.addPage();
                const heightPt = (endDom - startDom) * ptPerDomPx;
                pdf.addImage(slice.toDataURL('image/png'), 'PNG', margin, margin, printWidth, heightPt);
            }

            pdf.save('vouchers.pdf');

        } catch (err) {
            console.error('PDF generation failed:', err);
            alert('PDF generation failed: ' + err.message);
        } finally {
            area.setAttribute('style', savedStyle);
        }
    }
};

// ── Image Capture (camera) ───────────────────────────────────────────────────
window.imageCaptureApp = {
    _stream: null,

    startCamera: async function (videoId) {
        try {
            const video = document.getElementById(videoId);
            if (!video) return false;
            const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
            window.imageCaptureApp._stream = stream;
            video.srcObject = stream;
            return true;
        } catch (err) {
            console.error('Camera start error:', err);
            return false;
        }
    },

    stopCamera: function (videoId) {
        if (window.imageCaptureApp._stream) {
            window.imageCaptureApp._stream.getTracks().forEach(function (track) { track.stop(); });
            window.imageCaptureApp._stream = null;
        }
        const video = document.getElementById(videoId);
        if (video) { video.srcObject = null; }
    },

    captureFrame: function (videoId, canvasId, thumbWidth) {
        const video = document.getElementById(videoId);
        const canvas = document.getElementById(canvasId);
        if (!video || !canvas || !video.videoWidth) return null;

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height);

        const fullDataUrl = canvas.toDataURL('image/png');

        const ratio = video.videoHeight / video.videoWidth;
        const tWidth = Math.max(80, Number(thumbWidth) || 220);
        const tHeight = Math.max(60, Math.round(tWidth * ratio));

        const thumbCanvas = document.createElement('canvas');
        thumbCanvas.width = tWidth;
        thumbCanvas.height = tHeight;
        thumbCanvas.getContext('2d').drawImage(video, 0, 0, tWidth, tHeight);
        const thumbnailDataUrl = thumbCanvas.toDataURL('image/png');

        return {
            fullDataUrl,
            thumbnailDataUrl
        };
    }
};
