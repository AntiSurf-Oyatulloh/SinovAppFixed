document.addEventListener('DOMContentLoaded', function ()
{
    const searchForm = document.getElementById('searchForm');
    const searchQuery = document.getElementById('searchQuery');
    const searchResults = document.getElementById('searchResults');

    let searchTimeout;

    // Debounce function
    function debounce(func, wait)
    {
        return function executedFunction(...args)
        {
            const later = () =>
            {
                clearTimeout(searchTimeout);
                func(...args);
            };
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(later, wait);
        };
    }

    // Perform search
    const performSearch = debounce(async function ()
    {
        const query = searchQuery.value;

        if (searchResults)
        {
            searchResults.innerHTML = '<div class="text-center"><div class="spinner-border text-primary" role="status"></div></div>';
        }

        try
        {
            const response = await fetch(`/Books/Search?query=${encodeURIComponent(query)}`, {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok)
            {
                throw new Error('Network response was not ok');
            }

            const html = await response.text();

            if (searchResults)
            {
                if (html.trim() === '')
                {
                    searchResults.innerHTML = '<div class="alert alert-info text-center">Hech qanday kitob topilmadi</div>';
                } else
                {
                    searchResults.innerHTML = html;
                }
            }
        } catch (error)
        {
            console.error('Error:', error);
            if (searchResults)
            {
                searchResults.innerHTML = '<div class="alert alert-danger">Xatolik yuz berdi. Iltimos, qaytadan urinib ko\'ring.</div>';
            }
        }
    }, 300);

    // Add event listeners
    if (searchQuery)
    {
        searchQuery.addEventListener('input', performSearch);
    }

    if (searchForm)
    {
        searchForm.addEventListener('submit', function (e)
        {
            e.preventDefault();
            performSearch();
        });
    }

    // Initial search if there's a value
    if (searchQuery && searchQuery.value)
    {
        performSearch();
    }
});
