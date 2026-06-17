import { Avatar, AvatarGroup as MuiAvatarGroup, Tooltip } from '@mui/material';

interface UserAvatarProps {
  name?: string | null;
  avatarUrl?: string | null;
  size?: number;
}

export function UserAvatar({ name, avatarUrl, size = 32 }: UserAvatarProps) {
  const initials = name
    ? name.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase()
    : '?';
  return (
    <Tooltip title={name ?? ''}>
      <Avatar src={avatarUrl ?? undefined} sx={{ width: size, height: size, fontSize: size * 0.4 }}>
        {initials}
      </Avatar>
    </Tooltip>
  );
}

interface AvatarGroupProps {
  users: Array<{ name?: string | null; avatarUrl?: string | null }>;
  max?: number;
  size?: number;
}

export function AvatarGroup({ users, max = 4, size = 28 }: AvatarGroupProps) {
  return (
    <MuiAvatarGroup max={max} sx={{ '& .MuiAvatar-root': { width: size, height: size, fontSize: size * 0.35 } }}>
      {users.map((u, i) => (
        <UserAvatar key={i} name={u.name} avatarUrl={u.avatarUrl} size={size} />
      ))}
    </MuiAvatarGroup>
  );
}
