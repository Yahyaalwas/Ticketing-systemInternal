import {
  Box, Typography, IconButton, LinearProgress, Chip, Tooltip,
  Card, CardContent, Grid, Button, CircularProgress,
} from '@mui/material';
import {
  AttachFile as AttachIcon, Download as DownloadIcon,
  Delete as DeleteIcon, Image as ImageIcon,
  Description as DocIcon, CloudUpload as UploadIcon,
} from '@mui/icons-material';
import { useState, useRef, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ticketsApi } from '@/api/tickets';
import { formatBytes } from '@/utils/format';
import { formatDate } from '@/utils/date';
import { useNotification } from '@/hooks/useNotification';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';

interface AttachmentPanelProps {
  ticketId: string;
  readonly?: boolean;
}

function getFileIcon(contentType: string) {
  if (contentType.startsWith('image/')) return <ImageIcon />;
  return <DocIcon />;
}

export function AttachmentPanel({ ticketId, readonly = false }: AttachmentPanelProps) {
  const qc = useQueryClient();
  const { success, error } = useNotification();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [uploadingFiles, setUploadingFiles] = useState<string[]>([]);
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null);

  const { data: attachments = [], isLoading } = useQuery({
    queryKey: ['attachments', ticketId],
    queryFn: () => ticketsApi.getAttachments(ticketId),
    staleTime: 60_000,
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => ticketsApi.uploadAttachment(ticketId, file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['attachments', ticketId] });
      success('File uploaded');
    },
    onError: (err: Error) => error(err.message),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => ticketsApi.deleteAttachment(ticketId, id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['attachments', ticketId] });
      success('Attachment removed');
      setDeleteTarget(null);
    },
    onError: (err: Error) => error(err.message),
  });

  const handleFiles = useCallback((files: FileList | null) => {
    if (!files) return;
    Array.from(files).forEach(f => {
      setUploadingFiles(prev => [...prev, f.name]);
      uploadMutation.mutateAsync(f).finally(() =>
        setUploadingFiles(prev => prev.filter(n => n !== f.name))
      );
    });
  }, [uploadMutation]);

  const onDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    handleFiles(e.dataTransfer.files);
  };

  return (
    <Box>
      {/* Drop zone */}
      {!readonly && (
        <Box
          onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
          onDragLeave={() => setIsDragging(false)}
          onDrop={onDrop}
          onClick={() => fileInputRef.current?.click()}
          sx={{
            border: '2px dashed',
            borderColor: isDragging ? 'primary.main' : 'divider',
            borderRadius: 2,
            p: 2.5,
            mb: 2,
            textAlign: 'center',
            cursor: 'pointer',
            bgcolor: isDragging ? 'action.selected' : 'action.hover',
            transition: 'all 0.15s',
            '&:hover': { borderColor: 'primary.main', bgcolor: 'action.selected' },
          }}
        >
          <UploadIcon sx={{ fontSize: 32, color: 'text.disabled', mb: 0.5 }} />
          <Typography variant="body2" color="text.secondary">
            Drop files here or <span style={{ color: 'inherit', fontWeight: 600, textDecoration: 'underline' }}>browse</span>
          </Typography>
          <Typography variant="caption" color="text.disabled">Max 25 MB · PDF, Office, Images</Typography>
          <input
            ref={fileInputRef}
            type="file"
            multiple
            style={{ display: 'none' }}
            onChange={e => handleFiles(e.target.files)}
          />
        </Box>
      )}

      {/* Uploading progress */}
      {uploadingFiles.map(name => (
        <Box key={name} sx={{ mb: 1 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.25 }}>
            <Typography variant="caption">{name}</Typography>
            <Typography variant="caption">Uploading...</Typography>
          </Box>
          <LinearProgress />
        </Box>
      ))}

      {/* Attachments grid */}
      {isLoading ? (
        <Grid container spacing={1}>
          {[1, 2].map(i => (
            <Grid key={i} size={{ xs: 12, sm: 6 }}>
              <Card variant="outlined" sx={{ p: 1.5 }}>
                <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                  <Box sx={{ width: 36, height: 36, borderRadius: 1, bgcolor: 'action.hover' }} />
                  <Box sx={{ flex: 1 }}>
                    <Box sx={{ height: 14, bgcolor: 'action.hover', borderRadius: 0.5, mb: 0.5 }} />
                    <Box sx={{ height: 12, width: '60%', bgcolor: 'action.hover', borderRadius: 0.5 }} />
                  </Box>
                </Box>
              </Card>
            </Grid>
          ))}
        </Grid>
      ) : attachments.length === 0 ? (
        <Box sx={{ textAlign: 'center', py: 2 }}>
          <AttachIcon sx={{ fontSize: 32, color: 'text.disabled' }} />
          <Typography variant="body2" color="text.disabled">No attachments</Typography>
        </Box>
      ) : (
        <Grid container spacing={1}>
          {attachments.map(att => (
            <Grid key={att.id} size={{ xs: 12, sm: 6 }}>
              <Card variant="outlined" sx={{
                '&:hover': { borderColor: 'primary.main', bgcolor: 'action.hover' }, transition: 'all 0.15s',
              }}>
                <CardContent sx={{ p: '10px !important', display: 'flex', gap: 1, alignItems: 'flex-start' }}>
                  {att.contentType.startsWith('image/') ? (
                    <Box
                      component="img"
                      src={att.publicUrl}
                      alt={att.fileName}
                      sx={{ width: 40, height: 40, objectFit: 'cover', borderRadius: 1, flexShrink: 0 }}
                    />
                  ) : (
                    <Box sx={{
                      width: 40, height: 40, borderRadius: 1, bgcolor: 'action.selected',
                      display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, color: 'primary.main',
                    }}>
                      {getFileIcon(att.contentType)}
                    </Box>
                  )}
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Tooltip title={att.fileName}>
                      <Typography variant="body2" fontWeight={500} noWrap>{att.fileName}</Typography>
                    </Tooltip>
                    <Typography variant="caption" color="text.secondary">
                      {formatBytes(att.fileSizeBytes)} · {att.uploaderName} · {formatDate(att.uploadedAt)}
                    </Typography>
                  </Box>
                  <Box sx={{ display: 'flex', gap: 0.25, flexShrink: 0 }}>
                    <Tooltip title="Download">
                      <IconButton size="small" href={att.publicUrl} target="_blank" rel="noopener noreferrer">
                        <DownloadIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    {!readonly && (
                      <Tooltip title="Remove">
                        <IconButton size="small" color="error" onClick={() => setDeleteTarget(att.id)}>
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <ConfirmDialog
        open={!!deleteTarget}
        title="Remove Attachment"
        message="Are you sure you want to remove this attachment? This cannot be undone."
        confirmLabel="Remove"
        destructive
        loading={deleteMutation.isPending}
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget)}
        onCancel={() => setDeleteTarget(null)}
      />
    </Box>
  );
}
