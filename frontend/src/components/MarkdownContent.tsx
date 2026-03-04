'use client';

import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { oneDark } from 'react-syntax-highlighter/dist/esm/styles/prism';

interface MarkdownContentProps {
    content: string;
    className?: string;
}

export default function MarkdownContent({ content, className = '' }: MarkdownContentProps) {
    return (
        <div className={`markdown-content ${className}`}>
            <ReactMarkdown
                remarkPlugins={[remarkGfm]}
                components={{
                    code({ node, className, children, ...props }) {
                        const isInline = !className;
                        const match = /language-(\w+)/.exec(className || '');
                        const language = match ? match[1] : '';

                        if (isInline) {
                            return (
                                <code
                                    className="inline-code"
                                    style={{
                                        backgroundColor: 'rgba(0, 0, 0, 0.1)',
                                        padding: '0.2em 0.4em',
                                        borderRadius: '4px',
                                        fontSize: '0.9em',
                                        fontFamily: 'monospace',
                                    }}
                                    {...props}
                                >
                                    {children}
                                </code>
                            );
                        }

                        return (
                            <div style={{ margin: '1rem 0', borderRadius: '8px', overflow: 'hidden' }}>
                                {language && (
                                    <div
                                        style={{
                                            backgroundColor: '#2d2d2d',
                                            color: '#9ca3af',
                                            padding: '0.5rem 1rem',
                                            fontSize: '0.75rem',
                                            fontWeight: 500,
                                            textTransform: 'uppercase',
                                            letterSpacing: '0.05em',
                                        }}
                                    >
                                        {language}
                                    </div>
                                )}
                                <SyntaxHighlighter
                                    style={oneDark}
                                    language={language || 'text'}
                                    PreTag="div"
                                    customStyle={{
                                        margin: 0,
                                        borderRadius: language ? '0 0 8px 8px' : '8px',
                                        padding: '1rem',
                                    }}
                                >
                                    {String(children).replace(/\n$/, '')}
                                </SyntaxHighlighter>
                            </div>
                        );
                    },
                    p({ children }) {
                        return <p style={{ marginBottom: '1rem', lineHeight: '1.7' }}>{children}</p>;
                    },
                    h1({ children }) {
                        return <h1 style={{ fontSize: '1.5rem', fontWeight: 'bold', marginBottom: '1rem' }}>{children}</h1>;
                    },
                    h2({ children }) {
                        return <h2 style={{ fontSize: '1.25rem', fontWeight: 'bold', marginBottom: '0.75rem' }}>{children}</h2>;
                    },
                    h3({ children }) {
                        return <h3 style={{ fontSize: '1.1rem', fontWeight: '600', marginBottom: '0.5rem' }}>{children}</h3>;
                    },
                    ul({ children }) {
                        return <ul style={{ marginBottom: '1rem', paddingLeft: '1.5rem', listStyleType: 'disc' }}>{children}</ul>;
                    },
                    ol({ children }) {
                        return <ol style={{ marginBottom: '1rem', paddingLeft: '1.5rem', listStyleType: 'decimal' }}>{children}</ol>;
                    },
                    li({ children }) {
                        return <li style={{ marginBottom: '0.25rem' }}>{children}</li>;
                    },
                    blockquote({ children }) {
                        return (
                            <blockquote
                                style={{
                                    borderLeft: '4px solid var(--primary, #137fec)',
                                    paddingLeft: '1rem',
                                    marginLeft: 0,
                                    marginBottom: '1rem',
                                    color: 'var(--text-muted, #6b7280)',
                                    fontStyle: 'italic',
                                }}
                            >
                                {children}
                            </blockquote>
                        );
                    },
                    a({ children, href }) {
                        return (
                            <a
                                href={href}
                                style={{
                                    color: 'var(--primary, #137fec)',
                                    textDecoration: 'underline',
                                }}
                                target="_blank"
                                rel="noopener noreferrer"
                            >
                                {children}
                            </a>
                        );
                    },
                }}
            >
                {content}
            </ReactMarkdown>
        </div>
    );
}
