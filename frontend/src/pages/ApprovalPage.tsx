import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Check, X, Eye } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Table, Column } from '@/components/ui/Table'
import { Badge } from '@/components/ui/Badge'
import { Modal } from '@/components/ui/Modal'
import { Textarea } from '@/components/ui/Textarea'
import { PageLoading } from '@/components/ui/Loading'
import { Alert } from '@/components/ui/Alert'
import { approvalService, loanRequestService } from '@/services'
import { formatDate } from '@/lib/utils'
import type { PendingApproval, LoanRequest } from '@/types'

interface ApprovalPageProps {
  level: 'l1' | 'l2'
}

const approvalSchema = z.object({
  remarks: z.string().max(500, 'Catatan maksimal 500 karakter').optional()
})

type ApprovalFormData = z.infer<typeof approvalSchema>

export default function ApprovalPage({ level }: ApprovalPageProps) {
  const queryClient = useQueryClient()
  const [selectedRequest, setSelectedRequest] = useState<PendingApproval | null>(null)
  const [detailRequest, setDetailRequest] = useState<LoanRequest | null>(null)
  const [actionType, setActionType] = useState<'approve' | 'reject' | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { register, handleSubmit, reset, formState: { errors } } = useForm<ApprovalFormData>({
    resolver: zodResolver(approvalSchema)
  })

  const { data: pendingApprovals, isLoading } = useQuery({
    queryKey: ['pending-approvals', level],
    queryFn: async () => {
      const response = level === 'l1'
        ? await approvalService.getPendingL1()
        : await approvalService.getPendingL2()
      return response.data || []
    }
  })

  const approveMutation = useMutation({
    mutationFn: async ({ loanRequestId, remarks }: { loanRequestId: number; remarks?: string }) => {
      const processApproval = level === 'l1'
        ? approvalService.processL1
        : approvalService.processL2
      return processApproval({
        loanRequestId,
        status: 'APPROVED',
        remarks
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-approvals', level] })
      closeModal()
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } }
      setError(error.response?.data?.message || 'Gagal memproses approval')
    }
  })

  const rejectMutation = useMutation({
    mutationFn: async ({ loanRequestId, remarks }: { loanRequestId: number; remarks?: string }) => {
      const processApproval = level === 'l1'
        ? approvalService.processL1
        : approvalService.processL2
      return processApproval({
        loanRequestId,
        status: 'REJECTED',
        remarks
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['pending-approvals', level] })
      closeModal()
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } }
      setError(error.response?.data?.message || 'Gagal memproses penolakan')
    }
  })

  const handleViewDetail = async (item: PendingApproval) => {
    try {
      const response = await loanRequestService.getById(item.loanRequestId)
      setDetailRequest(response.data || null)
    } catch {
      setError('Gagal memuat detail pengajuan')
    }
  }

  const openApprovalModal = (item: PendingApproval, type: 'approve' | 'reject') => {
    setSelectedRequest(item)
    setActionType(type)
    setError(null)
    reset()
  }

  const closeModal = () => {
    setSelectedRequest(null)
    setActionType(null)
    setError(null)
    reset()
  }

  const onSubmit = (data: ApprovalFormData) => {
    if (!selectedRequest || !actionType) return

    if (actionType === 'approve') {
      approveMutation.mutate({
        loanRequestId: selectedRequest.loanRequestId,
        remarks: data.remarks
      })
    } else {
      rejectMutation.mutate({
        loanRequestId: selectedRequest.loanRequestId,
        remarks: data.remarks
      })
    }
  }

  const columns: Column<PendingApproval>[] = [
    {
      key: 'loanRequestId',
      header: 'ID',
      render: (item) => <span className="font-mono text-xs">#{item.loanRequestId}</span>
    },
    {
      key: 'requesterName',
      header: 'Pemohon',
      render: (item) => (
        <div>
          <p className="font-medium text-gray-900">{item.requesterName}</p>
          <p className="text-sm text-gray-500">{item.department}</p>
        </div>
      )
    },
    {
      key: 'purpose',
      header: 'Tujuan',
      render: (item) => (
        <div>
          <p className="font-medium text-gray-900">{item.purpose}</p>
          <p className="text-sm text-gray-500">{item.destination}</p>
        </div>
      )
    },
    {
      key: 'departureDate',
      header: 'Tanggal',
      render: (item) => (
        <div>
          <p className="text-sm">{formatDate(item.departureDate)}</p>
          <p className="text-xs text-gray-500">
            {item.departureTime} - {item.returnTime}
          </p>
        </div>
      )
    },
    {
      key: 'passengerCount',
      header: 'Penumpang',
      render: (item) => <span className="text-sm">{item.passengerCount} orang</span>
    },
    {
      key: 'status',
      header: 'Status',
      render: (item) => <Badge status={item.status} />
    },
    {
      key: 'actions',
      header: 'Aksi',
      render: (item) => (
        <div className="flex items-center gap-2">
          <Button 
            variant="ghost" 
            size="sm"
            onClick={() => handleViewDetail(item)}
          >
            <Eye className="w-4 h-4" />
          </Button>
          <Button 
            variant="secondary" 
            size="sm"
            onClick={() => openApprovalModal(item, 'approve')}
          >
            <Check className="w-4 h-4" />
          </Button>
          <Button 
            variant="danger" 
            size="sm"
            onClick={() => openApprovalModal(item, 'reject')}
          >
            <X className="w-4 h-4" />
          </Button>
        </div>
      )
    }
  ]

  if (isLoading) return <PageLoading />

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          Persetujuan Level {level === 'l1' ? '1' : '2'}
        </h1>
        <p className="text-gray-600">
          Kelola pengajuan yang memerlukan persetujuan Anda
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Menunggu Review</p>
            <p className="text-2xl font-bold text-yellow-600">{pendingApprovals?.length || 0}</p>
          </CardContent>
        </Card>
      </div>

      {/* Table */}
      <Card>
        <CardHeader>
          <CardTitle>Pengajuan Menunggu Approval</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table
            columns={columns}
            data={pendingApprovals || []}
            keyExtractor={(item) => item.loanRequestId}
            emptyMessage="Tidak ada pengajuan yang menunggu approval"
          />
        </CardContent>
      </Card>

      {/* Approval/Reject Modal */}
      <Modal
        isOpen={selectedRequest !== null && actionType !== null}
        onClose={closeModal}
        title={actionType === 'approve' ? 'Setujui Pengajuan' : 'Tolak Pengajuan'}
        description={`Pengajuan #${selectedRequest?.loanRequestId} - ${selectedRequest?.purpose}`}
      >
        {error && (
          <Alert variant="error" className="mb-4" onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="mb-4 p-4 bg-gray-50 rounded-lg">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-gray-500">Pemohon</p>
                <p className="font-medium">{selectedRequest?.requesterName}</p>
              </div>
              <div>
                <p className="text-gray-500">Departemen</p>
                <p className="font-medium">{selectedRequest?.department}</p>
              </div>
              <div>
                <p className="text-gray-500">Destinasi</p>
                <p className="font-medium">{selectedRequest?.destination}</p>
              </div>
              <div>
                <p className="text-gray-500">Tanggal</p>
                <p className="font-medium">
                  {selectedRequest && formatDate(selectedRequest.departureDate)}
                </p>
              </div>
            </div>
          </div>

          <Textarea
            label={actionType === 'approve' ? 'Catatan (opsional)' : 'Alasan Penolakan'}
            placeholder={actionType === 'approve' 
              ? 'Tambahkan catatan jika diperlukan...'
              : 'Jelaskan alasan penolakan...'
            }
            rows={3}
            error={errors.remarks?.message}
            {...register('remarks')}
          />

          <div className="flex justify-end gap-3 mt-6">
            <Button variant="ghost" type="button" onClick={closeModal}>
              Batal
            </Button>
            <Button
              type="submit"
              variant={actionType === 'approve' ? 'secondary' : 'danger'}
              isLoading={approveMutation.isPending || rejectMutation.isPending}
            >
              {actionType === 'approve' ? 'Setujui' : 'Tolak'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Detail Modal */}
      <Modal
        isOpen={detailRequest !== null}
        onClose={() => setDetailRequest(null)}
        title="Detail Pengajuan"
        size="lg"
      >
        {detailRequest && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm text-gray-500">ID Pengajuan</p>
                <p className="font-medium">#{detailRequest.id}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Status</p>
                <Badge status={detailRequest.status} />
              </div>
              <div>
                <p className="text-sm text-gray-500">Tujuan</p>
                <p className="font-medium">{detailRequest.purpose}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Destinasi</p>
                <p className="font-medium">{detailRequest.destination}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Tanggal</p>
                <p className="font-medium">{formatDate(detailRequest.departureDate)}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Waktu</p>
                <p className="font-medium">
                  {detailRequest.departureTime} - {detailRequest.returnTime}
                </p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Jumlah Penumpang</p>
                <p className="font-medium">{detailRequest.passengerCount} orang</p>
              </div>
            </div>
            {detailRequest.notes && (
              <div>
                <p className="text-sm text-gray-500">Catatan</p>
                <p className="text-gray-700 bg-gray-50 p-3 rounded-lg mt-1">
                  {detailRequest.notes}
                </p>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  )
}

// Export named components for routes
export function ApprovalL1Page() {
  return <ApprovalPage level="l1" />
}

export function ApprovalL2Page() {
  return <ApprovalPage level="l2" />
}
