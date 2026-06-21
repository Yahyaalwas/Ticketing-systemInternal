import {
  Box, Typography, IconButton, Tooltip, Chip, CircularProgress,
  Skeleton, Divider, Button,
} from '@mui/material';
import { Edit as EditIcon, Delete as DeleteIcon, History as HistoryIcon } from '@mui/icons-material';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ticketsApi } from '@/api/tickets';
import { CommentDto } from '@/types';
import { useNotification } from '@/hooks/useNotification';
import { useAuthStore } from '@/stores/authStore';
import { ConfirmDialog } from '@/components/common/ConfirmDialog';
import { UserAvatar } from '@/components/common/UserAvatar';
import { MarkdownRenderer } from './MarkdownRenderer';
import { CommentEditor } from './CommentEditor';
import { formatDistanceToNow, formatDateTime } from '@/utils/date';

interface SingleCommentProps {
  comment: CommentDto;
  ticketId: string;
  currentUserId?: string;
  onDeleted: () => void;
  onEdited: () => void;
}

function SingleComment({ comment, ticketId, currentUserId, onDeleted, onEdited }: SingleCommentProps) {
  const [editing, setEditing] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const { error } = useNotification();
  const qc = useQueryClient();

  const editMutation = useMutation({
    mutationFn: (body: string) => ticketsApi.editComment(ticketId, comment.id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['comments', ticketId] });
      setEditing(false);
      onEdited();
    },
    onError: (err: Error) => error(err.message),
  });

  const deleteMutation = useMutation({
    mutationFn: () => ticketsApi.deleteComment(ticketId, comment.id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['comments', ticketId] });
      setConfirmDelete(false);
      onDeleted();
    },
    onError: (err: Error) => error(err.message),
  });

  const isOwn = comment.authorUserId === currentUserId;

  return (
    <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'flex-start' }}>
      <UserAvatar name={comment.authorName} avatarUrl={comment.authorAvatarUrl} size={32} />
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5, flexWrap: 'wrap' }}>
          <Typography variant="body2" sx={{ fontWeight: 700 }}>{comment.authorName}</Typography>
          {comment.isEdited && (
            <Tooltip title="This comment was edited">
              <Chip icon={<HistoryIcon />} label="edited" size="small" variant="outlined" sx={{ height: 18, fontSize: 10 }} />
            </Tooltip>
          )}
          <Tooltip title={formatDateTime(comment.createdAt)}>
            <Typography variant="caption" color="text.disabled" sx={{ cursor: 'default' }}>
              {formatDistanceToNow(comment.createdAt)}
            </Typography>
          </Tooltip>
          {isOwn && !editing && (
            <Box sx={{ ml: 'auto', display: 'flex', gap: 0.25 }}>
              <Tooltip title="Edit">
                <IconButton size="small" onClick={() => setEditing(true)}>
                  <EditIcon sx={{ fontSize: 15 }} />
                </IconButton>
              </Tooltip>
              <Tooltip title="Delete">
                <IconButton size="small" color="error" onClick={() => setConfirmDelete(true)}>
                  <DeleteIcon sx={{ fontSize: 15 }} />
                </IconButton>
              </Tooltip>
            </Box>
          )}
        </Box>

        {editing ? (
          <CommentEditor
            ticketId={ticketId}
            initialValue={comment.body}
            submitLabel="Save Changes"
            onSubmit={(body) => editMutation.mutateAsync(body)}
            onCancel={() => setEditing(false)}
            loading={editMutation.isPending}
          />
        ) : (
          <Box sx={{
            bgcolor: 'action.hover', borderRadius: 2, p: 1.5,
            border: '1px solid', borderColor: 'divider',
          }}>
            <MarkdownRenderer html={comment.bodyHtml || comment.body} compact />
          </Box>
        )}
      </Box>

      <ConfirmDialog
        open={confirmDelete}
        title="Delete Comment"
        message="This comment will be permanently deleted."
        confirmLabel="Delete"
        destructive
        loading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setConfirmDelete(false)}
      />
    </Box>
  );
}

interface CommentsPanelProps {
  ticketId: string;
}

export function CommentsPanel({ ticketId }: CommentsPanelProps) {
  const qc = useQueryClient();
  const { user } = useAuthStore();
  const { success, error } = useNotification();
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['comments', ticketId, page],
    queryFn: () => ticketsApi.getComments(ticketId, page, 20),
    staleTime: 30_000,
  });

  const addMutation = useMutation({
    mutationFn: (body: string) => ticketsApi.addComment(ticketId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['comments', ticketId] });
      success('Comment added');
    },
    onError: (err: Error) => error(err.message),
  });

  const comments = data?.items ?? [];

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
          Comments {data?.totalCount ? `(${data.totalCount})` : ''}
        </Typography>
      </Box>

      {isLoading ? (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {[1, 2].map(i => (
            <Box key={i} sx={{ display: 'flex', gap: 1.5 }}>
              <Skeleton variant="circular" width={32} height={32} />
              <Box sx={{ flex: 1 }}>
                <Skeleton width="40%" height={18} sx={{ mb: 0.5 }} />
                <Skeleton variant="rectangular" height={60} sx={{ borderRadius: 1 }} />
              </Box>
            </Box>
          ))}
        </Box>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {comments.map((c) => (
            <SingleComment
              key={c.id}
              comment={c}
              ticketId={ticketId}
              currentUserId={user?.userId}
              onDeleted={() => qc.invalidateQueries({ queryKey: ['comments', ticketId] })}
              onEdited={() => qc.invalidateQueries({ queryKey: ['comments', ticketId] })}
            />
          ))}
          {comments.length === 0 && (
            <Box sx={{ textAlign: 'center', py: 3 }}>
              <Typography variant="body2" color="text.disabled">No comments yet. Be the first to comment.</Typography>
            </Box>
          )}
          {(data?.totalPages ?? 1) > 1 && (
            <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1 }}>
              <Button size="small" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</Button>
              <Typography variant="body2" sx={{ lineHeight: '30px' }}>{page} / {data?.totalPages}</Typography>
              <Button size="small" disabled={page >= (data?.totalPages ?? 1)} onClick={() => setPage(p => p + 1)}>Next</Button>
            </Box>
          )}
        </Box>
      )}

      <Divider sx={{ my: 3 }} />

      <Box sx={{ display: 'flex', gap: 1.5 }}>
        <UserAvatar name={user?.displayName} size={32} />
        <Box sx={{ flex: 1 }}>
          <CommentEditor
            ticketId={ticketId}
            onSubmit={(body) => addMutation.mutateAsync(body).then(() => undefined)}
            loading={addMutation.isPending}
          />
        </Box>
      </Box>
    </Box>
  );
}
