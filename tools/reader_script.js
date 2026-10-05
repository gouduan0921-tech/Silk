(() => {
      "use strict";
      const body = document.body;
      const chapters = [...document.querySelectorAll(".chapter")];
      const links = [...document.querySelectorAll(".nav-link")];
      const search = document.getElementById("search");
      const searchStatus = document.getElementById("search-status");
      const noResults = document.getElementById("no-results");
      const previousButton = document.getElementById("previous");
      const nextButton = document.getElementById("next");
      const showAllButton = document.getElementById("show-all");
      const mobileNavButton = document.getElementById("mobile-nav-button");
      const navScrim = document.getElementById("nav-scrim");
      let showAll = false;
      let visibleChapter = chapters[0];

      const decodeHash = () => {
        try { return decodeURIComponent(location.hash.slice(1)); }
        catch (_) { return ""; }
      };

      const chapterForId = (id) => chapters.find((chapter) => chapter.id === id);
      const linkForChapter = (chapter) =>
        links.find((link) => decodeURIComponent(link.hash.slice(1)) === chapter.id);

      function closeMobileNav() {
        body.classList.remove("nav-open");
        mobileNavButton.setAttribute("aria-expanded", "false");
      }

      function updateNavigation(chapter) {
        visibleChapter = chapter || chapters[0];
        links.forEach((link) => {
          const active = link === linkForChapter(visibleChapter);
          link.classList.toggle("active", active);
          if (active) link.setAttribute("aria-current", "page");
          else link.removeAttribute("aria-current");
        });
        const index = chapters.indexOf(visibleChapter);
        previousButton.disabled = showAll || index <= 0;
        nextButton.disabled = showAll || index < 0 || index >= chapters.length - 1;
      }

      function selectChapter({ focus = false, scroll = false } = {}) {
        showAll = false;
        body.classList.remove("show-all");
        showAllButton.textContent = "展开全文";
        showAllButton.setAttribute("aria-pressed", "false");
        const selected = chapterForId(decodeHash()) || chapters[0];
        chapters.forEach((chapter) => chapter.classList.toggle("active", chapter === selected));
        updateNavigation(selected);
        if (scroll) window.scrollTo({ top: 0, behavior: "auto" });
        if (focus) selected.focus({ preventScroll: true });
      }

      function navigateBy(offset) {
        const current = chapters.indexOf(visibleChapter);
        const target = chapters[current + offset];
        if (!target) return;
        search.value = "";
        filterChapters();
        location.hash = target.id;
      }

      function filterChapters() {
        const query = search.value.trim().toLocaleLowerCase("zh-CN");
        if (!query) {
          links.forEach((link) => { link.hidden = false; });
          searchStatus.textContent = "";
          noResults.style.display = "none";
          if (showAll) {
            chapters.forEach((chapter) => chapter.classList.remove("active"));
            updateNavigation(visibleChapter);
          } else {
            selectChapter();
          }
          return;
        }

        showAll = false;
        body.classList.remove("show-all");
        showAllButton.textContent = "展开全文";
        showAllButton.setAttribute("aria-pressed", "false");
        const matches = [];
        chapters.forEach((chapter, index) => {
          const haystack = `${chapter.dataset.title} ${chapter.textContent}`
            .toLocaleLowerCase("zh-CN");
          const matched = haystack.includes(query);
          links[index].hidden = !matched;
          chapter.classList.toggle("active", false);
          if (matched) matches.push(chapter);
        });
        searchStatus.textContent = matches.length
          ? `找到 ${matches.length} 个相关章节`
          : "没有匹配章节";
        noResults.style.display = matches.length ? "none" : "block";
        if (matches[0]) {
          matches[0].classList.add("active");
          updateNavigation(matches[0]);
        } else {
          links.forEach((link) => {
            link.classList.remove("active");
            link.removeAttribute("aria-current");
          });
          previousButton.disabled = true;
          nextButton.disabled = true;
        }
      }

      window.addEventListener("hashchange", () => {
        search.value = "";
        links.forEach((link) => { link.hidden = false; });
        searchStatus.textContent = "";
        noResults.style.display = "none";
        selectChapter({ focus: true, scroll: true });
        closeMobileNav();
      });

      document.addEventListener("click", (event) => {
        const anchor = event.target.closest('a[href^="#"]');
        if (!anchor) return;
        const id = decodeURIComponent(anchor.hash.slice(1));
        if (!chapterForId(id)) return;
        event.preventDefault();
        search.value = "";
        if (decodeHash() === id) {
          filterChapters();
          selectChapter({ focus: true, scroll: true });
        } else {
          location.hash = id;
        }
      });

      search.addEventListener("input", filterChapters);
      previousButton.addEventListener("click", () => navigateBy(-1));
      nextButton.addEventListener("click", () => navigateBy(1));
      document.getElementById("print").addEventListener("click", () => window.print());
      showAllButton.addEventListener("click", () => {
        search.value = "";
        links.forEach((link) => { link.hidden = false; });
        searchStatus.textContent = "";
        noResults.style.display = "none";
        showAll = !showAll;
        body.classList.toggle("show-all", showAll);
        showAllButton.textContent = showAll ? "按章节阅读" : "展开全文";
        showAllButton.setAttribute("aria-pressed", String(showAll));
        if (showAll) {
          chapters.forEach((chapter) => chapter.classList.remove("active"));
          updateNavigation(visibleChapter);
          window.scrollTo({ top: 0, behavior: "auto" });
        } else {
          selectChapter({ scroll: true });
        }
      });

      mobileNavButton.addEventListener("click", () => {
        const open = body.classList.toggle("nav-open");
        mobileNavButton.setAttribute("aria-expanded", String(open));
        if (open) search.focus();
      });
      navScrim.addEventListener("click", closeMobileNav);

      document.addEventListener("keydown", (event) => {
        const typing = /^(INPUT|TEXTAREA|SELECT)$/.test(document.activeElement.tagName);
        if (event.key === "/" && !typing) {
          event.preventDefault();
          search.focus();
        } else if (event.key === "Escape") {
          if (search.value) {
            search.value = "";
            filterChapters();
            search.focus();
          }
          closeMobileNav();
        } else if (event.altKey && event.key === "ArrowLeft") {
          event.preventDefault();
          navigateBy(-1);
        } else if (event.altKey && event.key === "ArrowRight") {
          event.preventDefault();
          navigateBy(1);
        }
      });

      selectChapter();
    })();