/***** Types *****/

interface IProps {
  page: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
  onChange: (page: number) => void;
}

/***** Components *****/

/** Default component: moves between pages, and says where the reader is. */
function Pagination(props: IProps) {
  const { page, totalPages, hasPrevious, hasNext, onChange } = props;

  if (totalPages <= 1) {
    return null;
  }

  return (
    <nav aria-label="Pagination" className="flex items-center justify-center gap-3 py-4 text-sm">
      <button
        className="rounded-sm border border-line px-3 py-1.5 disabled:opacity-40"
        disabled={!hasPrevious}
        onClick={() => onChange(page - 1)}
        type="button"
      >
        Previous
      </button>
      <span aria-live="polite" className="font-mono text-muted tabular-nums">
        Page {page} of {totalPages}
      </span>
      <button
        className="rounded-sm border border-line px-3 py-1.5 disabled:opacity-40"
        disabled={!hasNext}
        onClick={() => onChange(page + 1)}
        type="button"
      >
        Next
      </button>
    </nav>
  );
}

/***** Export default *****/

export default Pagination;
