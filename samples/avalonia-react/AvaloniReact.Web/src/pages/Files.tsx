import { useState, useEffect } from 'react';
import { Folder, File, ChevronRight, ArrowUp, Eye } from 'lucide-react';
import { fileService, type FileEntry } from '../bridge/services';
import { useI18n } from '../i18n/I18nContext';

export function Files() {
  const { t } = useI18n();
  const [entries, setEntries] = useState<FileEntry[]>([]);
  const [currentPath, setCurrentPath] = useState<string>('');
  const [preview, setPreview] = useState<{ name: string; content: string } | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fileService.getUserDocumentsPath().then((p) => {
      setCurrentPath(p);
      loadFiles(p);
    }).catch(() => setLoading(false));
  }, []);

  const loadFiles = async (path: string) => {
    setLoading(true);
    setPreview(null);
    try {
      const files = await fileService.listFiles(path);
      setEntries(files);
    } catch {
      setEntries([]);
    } finally {
      setLoading(false);
    }
  };

  const navigateTo = (path: string) => {
    setCurrentPath(path);
    loadFiles(path);
  };

  const goUp = () => {
    const parent = currentPath.replace(/[/\\][^/\\]*$/, '');
    if (parent && parent !== currentPath) navigateTo(parent);
  };

  const openPreview = async (entry: FileEntry) => {
    if (entry.isDirectory) {
      navigateTo(entry.path);
      return;
    }
    const content = await fileService.readTextFile(entry.path);
    setPreview({ name: entry.name, content });
  };

  const pathParts = currentPath.split(/[/\\]/).filter(Boolean);

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-800 shrink-0">
        <h1 className="text-lg font-bold">{t('files.title')}</h1>
        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
          {t('files.subtitle')} <code className="bg-gray-100 dark:bg-gray-800 px-1 rounded">[JsExport] IFileService</code>
        </p>
      </div>

      {/* Breadcrumb */}
      <div className="flex items-center gap-1 px-6 py-2 text-xs border-b border-gray-100 dark:border-gray-800 overflow-x-auto shrink-0">
        <button onClick={goUp} className="p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-800">
          <ArrowUp className="w-3.5 h-3.5" />
        </button>
        {pathParts.map((part, i) => (
          <span key={i} className="flex items-center gap-1">
            <ChevronRight className="w-3 h-3 text-gray-300 dark:text-gray-600" />
            <button
              className="hover:text-blue-500 truncate max-w-32"
              onClick={() => navigateTo('/' + pathParts.slice(0, i + 1).join('/'))}
            >
              {part}
            </button>
          </span>
        ))}
      </div>

      <div className="flex flex-1 overflow-hidden">
        {/* File list */}
        <div className={`overflow-auto ${preview ? 'w-1/2 border-r border-gray-200 dark:border-gray-800' : 'w-full'}`}>
          {loading ? (
            <div className="flex items-center justify-center py-16 text-gray-400">
              <div className="w-5 h-5 border-2 border-gray-300 border-t-blue-500 rounded-full animate-spin" />
            </div>
          ) : entries.length === 0 ? (
            <p className="text-sm text-gray-400 text-center py-16">{t('files.empty')}</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 dark:border-gray-800 text-xs text-gray-500 dark:text-gray-400">
                  <th className="text-left px-5 py-2 font-medium">{t('files.name')}</th>
                  <th className="text-right px-5 py-2 font-medium w-24">{t('files.size')}</th>
                  <th className="text-right px-5 py-2 font-medium w-40">{t('files.modified')}</th>
                  <th className="w-12" />
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => (
                  <tr
                    key={entry.path}
                    className="border-b border-gray-50 dark:border-gray-800/50 hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer"
                    onClick={() => openPreview(entry)}
                  >
                    <td className="px-5 py-2 flex items-center gap-2">
                      {entry.isDirectory ? (
                        <Folder className="w-4 h-4 text-blue-500 shrink-0" />
                      ) : (
                        <File className="w-4 h-4 text-gray-400 shrink-0" />
                      )}
                      <span className="truncate">{entry.name}</span>
                    </td>
                    <td className="text-right px-5 py-2 text-gray-500 dark:text-gray-400 tabular-nums">
                      {entry.isDirectory ? '—' : formatSize(entry.size)}
                    </td>
                    <td className="text-right px-5 py-2 text-gray-500 dark:text-gray-400">
                      {formatDate(entry.lastModified)}
                    </td>
                    <td className="px-2">
                      {!entry.isDirectory && (
                        <Eye className="w-3.5 h-3.5 text-gray-300 dark:text-gray-600" />
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* Preview panel */}
        {preview && (
          <div className="w-1/2 flex flex-col overflow-hidden">
            <div className="flex items-center justify-between px-4 py-2 border-b border-gray-200 dark:border-gray-800 shrink-0">
              <span className="text-xs font-medium truncate">{preview.name}</span>
              <button
                onClick={() => setPreview(null)}
                className="text-xs text-gray-400 hover:text-gray-600"
              >
                {t('files.close')}
              </button>
            </div>
            <pre className="flex-1 overflow-auto p-4 text-xs font-mono text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-900/50 whitespace-pre-wrap">
              {preview.content}
            </pre>
          </div>
        )}
      </div>
    </div>
  );
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString(undefined, {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  } catch {
    return iso;
  }
}
