'use client';

/**
 * AskQuestionEditor - Trình soạn thảo Markdown cho form Ask a Question
 * Sử dụng @uiw/react-md-editor với dynamic import để tránh lỗi SSR (Next.js App Router)
 * preview="edit" mặc định full-width; toolbar cho phép chuyển sang live/preview nếu cần
 * highlightEnable={false} để tránh raw HTML và blue highlight trong cột soạn thảo
 */
import dynamic from 'next/dynamic';
import '@uiw/react-md-editor/markdown-editor.css';
import '@uiw/react-markdown-preview/markdown.css';

// Dynamic import tránh lỗi SSR do MDEditor dùng browser APIs
const MDEditor = dynamic(() => import('@uiw/react-md-editor'), { ssr: false });

export interface AskQuestionEditorProps {
    value: string;
    onChange: (value: string) => void;
    colorMode: 'light' | 'dark';
}

export default function AskQuestionEditor({ value, onChange, colorMode }: AskQuestionEditorProps) {
    return (
        <div data-color-mode={colorMode} className="ask-question-editor flex min-h-0 w-full flex-1 flex-col">
            <MDEditor
                className="min-h-0 flex-1"
                value={value}
                onChange={(val) => onChange(val ?? '')}
                preview="edit"
                height="100%"
                highlightEnable={false}
                textareaProps={{
                    placeholder: 'Describe your problem in detail... (supports Markdown)',
                    'data-testid': 'question-body',
                } as any}
            />
        </div>
    );
}
